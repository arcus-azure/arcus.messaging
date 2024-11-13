using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Resiliency;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Arcus.Testing;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Bogus;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Sdk;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Arcus.Messaging.Tests.Integration.MessagePump.TestUnavailableDependencyAzureServiceBusMessageHandler;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public class TestUnavailableDependencyAzureServiceBusMessageHandler : CircuitBreakerServiceBusMessageHandler<Order>
    {
        public TestUnavailableDependencyAzureServiceBusMessageHandler(
            IMessagePumpCircuitBreaker circuitBreaker,
            ILogger<CircuitBreakerServiceBusMessageHandler<Order>> logger) : base(circuitBreaker, logger)
        {
        }

        public static readonly Collection<(Order order, DateTimeOffset timestamp)> TriedOrders = new();

        protected override Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            MessagePumpCircuitBreakerOptions options,
            CancellationToken cancellationToken)
        {
            options.MessageRecoveryPeriod = TimeSpan.FromSeconds(5);
            options.MessageIntervalDuringRecovery = TimeSpan.FromSeconds(1);

            Logger.LogDebug("Process order for the {DeliveryCount}nd time", messageContext.DeliveryCount);
            TriedOrders.Add((message, DateTimeOffset.UtcNow));

            if (TriedOrders.Count < 3)
            {
                throw new InvalidOperationException(
                    $"Simulate an unhealthy dependency system! (DeliveryCount: {messageContext.DeliveryCount})");
            }

            return Task.CompletedTask;
        }
    }

    public partial class ServiceBusMessagePumpTests
    {
        private string NamespaceConnectionString => _config["Arcus:ServiceBus:Docker:NamespaceConnectionString"];

        [Fact]
        public async Task ServiceBusMessageQueuePump_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable()
        {
            // Arrange
            TriedOrders.Clear();
            await using TemporaryQueue queue = await CreateQueueAsync();

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .ConfigureSerilog(logging => logging.MinimumLevel.Debug())
                   .AddServiceBusQueueMessagePump(queue.Name, _ => NamespaceConnectionString)
                   .WithServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler, Order>();

            var producer = new TestServiceBusMessageProducer($"{NamespaceConnectionString};EntityPath={queue.Name}");
            ServiceBusMessage messageBeforeBreak = CreateOrderServiceBusMessageForW3C();
            ServiceBusMessage messageAfterBreak = CreateOrderServiceBusMessageForW3C();

            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messageBeforeBreak);

            // Assert
            await Poll.Target(() => Assert.Equal(3, TriedOrders.Count));

            await producer.ProduceAsync(messageAfterBreak);
            await Poll.Target(() => Assert.Equal(4, TriedOrders.Count));
        }

        private async Task<TemporaryQueue> CreateQueueAsync()
        {
            var client = new ServiceBusAdministrationClient(NamespaceConnectionString);

            return await TemporaryQueue.CreateIfNotExistsAsync(client, $"queue-{Guid.NewGuid()}", _logger);
        }

        [Fact]
        public async Task ServiceBusMessageTopicPump_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable()
        {
            // Arrange
            TriedOrders.Clear();
            ServiceBusMessage messageBeforeBreak = CreateOrderServiceBusMessageForW3C();
            ServiceBusMessage messageAfterBreak = CreateOrderServiceBusMessageForW3C();
            await using TemporaryTopicSubscription subscription = await CreateTopicSubscriptionForMessageAsync(messageBeforeBreak, messageAfterBreak);

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .ConfigureSerilog(logging => logging.MinimumLevel.Debug())
                   .AddServiceBusTopicMessagePump(subscription.Name, _ => TopicConnectionString)
                   .WithServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler, Order>();

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messageBeforeBreak);

            // Assert
            await Poll.Target(() => Assert.Equal(3, TriedOrders.Count));

            await producer.ProduceAsync(messageAfterBreak);
            await Poll.Target(() => Assert.Equal(4, TriedOrders.Count));
        }

        private async Task<TemporaryTopicSubscription> CreateTopicSubscriptionForMessageAsync(params ServiceBusMessage[] messages)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(TopicConnectionString);
            var client = new ServiceBusAdministrationClient(properties.GetNamespaceConnectionString());

            return await TemporaryTopicSubscription.CreateIfNotExistsAsync(
                client, 
                properties.EntityPath,
                $"circuit-breaker-{Guid.NewGuid().ToString("N")[..10]}", 
                _logger,
                configureOptions: null, 
                rule: new CreateRuleOptions("MessageId", new SqlRuleFilter($"sys.messageid in ({string.Join(", ", messages.Select(m => $"'{m.MessageId}'"))})")));
        }

        [Fact]
        public async Task ServiceBusTopicMessagePump_PauseViaCircuitBreaker_RestartsAgainWithOneMessage()
        {
            // Arrange
            var options = new WorkerOptions();
            ServiceBusMessage[] messages = GenerateShipmentMessages(1);
            TimeSpan recoveryTime = TimeSpan.FromSeconds(10);
            TimeSpan messageInterval = TimeSpan.FromSeconds(2);

            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: "circuit-breaker-" + Guid.NewGuid(),
                       _ => _config.GetServiceBusTopicConnectionString(),
                       opt => opt.TopicSubscription = TopicSubscription.Automatic)
                   .WithServiceBusMessageHandler<TestCircuitBreakerAzureServiceBusMessageHandler, Shipment>(
                        implementationFactory: provider => new TestCircuitBreakerAzureServiceBusMessageHandler(
                            targetMessageIds: messages.Select(m => m.MessageId).ToArray(),
                            configureOptions: opt =>
                            {
                                opt.MessageRecoveryPeriod = recoveryTime;
                                opt.MessageIntervalDuringRecovery = messageInterval;
                            },
                            provider.GetRequiredService<IMessagePumpCircuitBreaker>(),
                            provider.GetRequiredService<ILogger<TestCircuitBreakerAzureServiceBusMessageHandler>>()));

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messages);

            // Assert
            await Task.Delay(TimeSpan.FromDays(1));

            var handler = GetMessageHandler<TestCircuitBreakerAzureServiceBusMessageHandler>(worker);
            AssertX.RetryAssertUntil(() =>
            {
                DateTimeOffset[] arrivals = handler.GetMessageArrivals();

                _outputWriter.WriteLine("Arrivals: {0}", string.Join(", ", arrivals));
                TimeSpan faultMargin = TimeSpan.FromSeconds(1);
                Assert.Collection(arrivals.SkipLast(1).Zip(arrivals.Skip(1)),
                    dates => AssertDateDiff(dates.First, dates.Second, recoveryTime, recoveryTime.Add(faultMargin)),
                    dates => AssertDateDiff(dates.First, dates.Second, messageInterval, messageInterval.Add(faultMargin)));

            }, timeout: TimeSpan.FromMinutes(2), _logger);

            var pump = Assert.IsType<AzureServiceBusMessagePump>(worker.Services.GetService<IHostedService>());
            Assert.True(pump.IsStarted, "pump should be started after circuit breaker scenario");
        }

        private static TMessageHandler GetMessageHandler<TMessageHandler>(Worker worker)
        {
            return Assert.IsType<TMessageHandler>(
                worker.Services.GetRequiredService<MessageHandler>()
                               .GetMessageHandlerInstance());
        }

        private static void AssertDateDiff(DateTimeOffset left, DateTimeOffset right, TimeSpan expectedMin, TimeSpan expectedMax)
        {
            left = new DateTimeOffset(left.Year, left.Month, left.Day, left.Hour, left.Minute, left.Second, 0, left.Offset);
            right = new DateTimeOffset(right.Year, right.Month, right.Day, right.Hour, right.Minute, right.Second, 0, right.Offset);

            TimeSpan actual = right - left;
            Assert.InRange(actual, expectedMin, expectedMax);
        }

        private static ServiceBusMessage[] GenerateShipmentMessages(int count)
        {
            var generator = new Faker<Shipment>()
                .RuleFor(s => s.Id, f => f.Random.Guid().ToString())
                .RuleFor(s => s.Code, f => f.Random.Int(1, 100))
                .RuleFor(s => s.Date, f => f.Date.RecentOffset())
                .RuleFor(s => s.Description, f => f.Lorem.Sentence());

            return Enumerable.Repeat(generator, count).Select(g =>
            {
                Shipment shipment = g.Generate();
                string json = JsonConvert.SerializeObject(shipment);
                return new ServiceBusMessage(json)
                {
                    MessageId = shipment.Id
                };
            }).ToArray();
        }

        [Fact]
        public async Task ServiceBusMessagePump_PauseViaLifetime_RestartsAgain()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            string jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString, 
                       opt =>
                       {
                           opt.JobId = jobId;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using var worker = await Worker.StartNewAsync(options);
            
            var lifetime = worker.Services.GetRequiredService<IMessagePumpLifetime>();
            await lifetime.PauseProcessingMessagesAsync(jobId, TimeSpan.FromSeconds(5), CancellationToken.None);

            // Act
            await producer.ProduceAsync(message);

            // Assert
            OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
            AssertReceivedOrderEventDataForW3C(message, eventData);
        }
    }

    /// <summary>
    /// Represents a temporary Azure Service Bus topic subscription that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryTopicSubscription : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _client;
        private readonly string _serviceBusNamespace;
        private readonly CreateSubscriptionOptions _options;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryTopicSubscription(
            ServiceBusAdministrationClient client,
            CreateSubscriptionOptions options,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);

            _client = client;
            _options = options;
            _createdByUs = createdByUs;
            _logger = logger;

            Name = _options.SubscriptionName;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus topic subscription that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic subscription should be created.</param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/>
        ///     in the given namespace where the given <paramref name="adminClient"/> points to.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string subscriptionName,
            ILogger logger,
            Action<CreateSubscriptionOptions> configureOptions,
            CreateRuleOptions rule)
        {
            ArgumentNullException.ThrowIfNull(adminClient);

            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus topic name to create a temporary topic subscription", nameof(topicName));
            }

            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus topic subscription name to create a temporary topic subscription", nameof(subscriptionName));
            }

            logger ??= NullLogger.Instance;

            var options = new CreateSubscriptionOptions(topicName, subscriptionName);
            configureOptions?.Invoke(options);


            if (!await adminClient.TopicExistsAsync(options.TopicName))
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] cannot create temporary subscription '{options.SubscriptionName}' on Azure Service Bus topic '{options.TopicName}' " +
                    $"because the topic '{options.TopicName}' does not exists in the provided Azure Service Bus namespace. " +
                    $"Please make sure to have an available Azure Service Bus topic before using the temporary topic subscription test fixture");
            }

            if (await adminClient.SubscriptionExistsAsync(options.TopicName, options.SubscriptionName))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service Bus topic subscription '{SubscriptionName}' in '{TopicName}'", options.SubscriptionName, options.TopicName);
                return new TemporaryTopicSubscription(adminClient, options, createdByUs: false, logger);
            }

            logger.LogTrace("[Test:Setup] Create new Azure Service Bus topic subscription '{SubscriptionName}' in '{TopicName}'", options.SubscriptionName, options.TopicName);
            await adminClient.CreateSubscriptionAsync(options, rule);

            return new TemporaryTopicSubscription(adminClient, options, createdByUs: true, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);

            if (_createdByUs && await _client.SubscriptionExistsAsync(_options.TopicName, _options.SubscriptionName))
            {
                _logger.LogTrace("[Test:Teardown] Delete Azure Service Bus topic subscription '{SubscriptionName}' in '{Namespace}/{TopicName}'", _options.SubscriptionName, _serviceBusNamespace, _options.TopicName);
                await _client.DeleteSubscriptionAsync(_options.TopicName, _options.SubscriptionName);
            }
        }
    }

    /// <summary>
    /// Represents a temporary Azure Service Bus queue that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryQueue : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _client;
        private readonly string _serviceBusNamespace;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryQueue(
            ServiceBusAdministrationClient client,
            string serviceBusNamespace,
            string queueName,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
            _serviceBusNamespace = serviceBusNamespace;
            _createdByUs = createdByUs;
            _logger = logger;

            Name = queueName;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus queue that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="queueName"/> is blank.
        /// </exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(string fullyQualifiedNamespace, string queueName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(fullyQualifiedNamespace, queueName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus queue should be created.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="queueName"/> is blank.
        /// </exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            string fullyQualifiedNamespace,
            string queueName,
            ILogger logger,
            Action<CreateQueueOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException(
                    "Requires a non-blank fully-qualified Azure Service bus namespace to set up a temporary queue", nameof(fullyQualifiedNamespace));
            }

            var client = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
            return await CreateIfNotExistsAsync(client, queueName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(ServiceBusAdministrationClient adminClient, string queueName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, queueName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus queue should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string queueName,
            ILogger logger,
            Action<CreateQueueOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus queue name to set up a temporary queue", nameof(queueName));
            }

            var options = new CreateQueueOptions(queueName);
            configureOptions?.Invoke(options);

            NamespaceProperties properties = await adminClient.GetNamespacePropertiesAsync();
            string serviceBusNamespace = properties.Name;

            if (await adminClient.QueueExistsAsync(options.Name))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service Bus queue '{QueueName}' in namespace '{Namespace}'", options.Name, serviceBusNamespace);
                return new TemporaryQueue(adminClient, serviceBusNamespace, options.Name, createdByUs: false, logger);
            }

            logger.LogTrace("[Test:Setup] Create new Azure Service Bus queue '{Queue}' in namespace '{Namespace}'", options.Name, serviceBusNamespace);
            await adminClient.CreateQueueAsync(options);

            return new TemporaryQueue(adminClient, serviceBusNamespace, options.Name, createdByUs: true, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_createdByUs && await _client.QueueExistsAsync(Name))
            {
                _logger.LogTrace("[Test:Teardown] Delete Azure Service Bus queue '{QueueName}' in namespace '{Namespace}'", Name, _serviceBusNamespace);
                await _client.DeleteQueueAsync(Name);
            }

            GC.SuppressFinalize(this);
        }
    }
}
