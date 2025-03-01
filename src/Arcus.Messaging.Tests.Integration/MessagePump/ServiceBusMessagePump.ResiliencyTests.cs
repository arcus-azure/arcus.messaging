﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
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
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Arcus.Messaging.Tests.Integration.MessagePump.TestUnavailableDependencyAzureServiceBusMessageHandler;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusMessageQueuePump_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable()
        {
            // Arrange
            var messageSink = new OrderMessageSink();
            var mockEventHandler1 = new MockCircuitBreakerEventHandler();
            var mockEventHandler2 = new MockCircuitBreakerEventHandler();

            ServiceBusMessage messageBeforeBreak = CreateOrderServiceBusMessageForW3C();
            ServiceBusMessage messageAfterBreak = CreateOrderServiceBusMessageForW3C();

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddSingleton(messageSink)
                   .AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName)
                   .WithServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler, Order>(
                       messageContextFilter: ctx => ctx.MessageId == messageBeforeBreak.MessageId
                                                    || ctx.MessageId == messageAfterBreak.MessageId)
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler1)
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler2);

            var producer = new TestServiceBusMessageProducer(QueueName, _config.GetServiceBus());
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messageBeforeBreak);

            // Assert
            await messageSink.ShouldReceiveOrdersDuringBreakAsync(messageBeforeBreak.MessageId);

            await producer.ProduceAsync(messageAfterBreak);
            await messageSink.ShouldReceiveOrdersAfterBreakAsync(messageAfterBreak.MessageId);

            await mockEventHandler1.ShouldTransitionedCorrectlyAsync();
            await mockEventHandler2.ShouldTransitionedCorrectlyAsync();
        }

        [Fact]
        public async Task ServiceBusMessageTopicPump_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable()
        {
            // Arrange
            ServiceBusMessage messageBeforeBreak = CreateOrderServiceBusMessageForW3C();
            ServiceBusMessage messageAfterBreak = CreateOrderServiceBusMessageForW3C();

            var messageSink = new OrderMessageSink();
            var mockEventHandler1 = new MockCircuitBreakerEventHandler();
            var mockEventHandler2 = new MockCircuitBreakerEventHandler();
            await using TemporaryTopicSubscription subscription = await CreateTopicSubscriptionForMessageAsync(messageBeforeBreak, messageAfterBreak);

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .ConfigureSerilog(logging => logging.MinimumLevel.Verbose())
                   .AddSingleton(messageSink)
                   .AddServiceBusTopicMessagePumpUsingManagedIdentity(TopicName, subscription.Name, HostName)
                   .WithServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler, Order>()
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler1)
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler2);

            var producer = new TestServiceBusMessageProducer(TopicName, _config.GetServiceBus());
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messageBeforeBreak);

            // Assert
            await messageSink.ShouldReceiveOrdersDuringBreakAsync(messageBeforeBreak.MessageId);

            await producer.ProduceAsync(messageAfterBreak);
            await messageSink.ShouldReceiveOrdersAfterBreakAsync(messageAfterBreak.MessageId);

            await mockEventHandler1.ShouldTransitionedCorrectlyAsync();
            await mockEventHandler2.ShouldTransitionedCorrectlyAsync();
        }

        private async Task<TemporaryTopicSubscription> CreateTopicSubscriptionForMessageAsync(params ServiceBusMessage[] messages)
        {
            var client = new ServiceBusAdministrationClient(NamespaceConnectionString);

            return await TemporaryTopicSubscription.CreateIfNotExistsAsync(
                client,
                TopicName,
                $"circuit-breaker-{Guid.NewGuid().ToString("N")[..10]}",
                _logger,
                configureOptions: null,
                rule: new CreateRuleOptions("MessageId", new SqlRuleFilter($"sys.messageid in ({string.Join(", ", messages.Select(m => $"'{m.MessageId}'"))})")));
        }

        [Fact]
        public async Task ServiceBusMessagePump_PauseViaLifetime_RestartsAgain()
        {
            // Arrange
            string jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePumpUsingManagedIdentity(
                       TopicName,
                       subscriptionName: Guid.NewGuid().ToString(),
                       HostName,
                       configureMessagePump: opt =>
                       {
                           opt.JobId = jobId;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            var producer = TestServiceBusMessageProducer.CreateFor(TopicName, _config);
            await using var worker = await Worker.StartNewAsync(options);

            var lifetime = worker.Services.GetRequiredService<IMessagePumpLifetime>();
            await lifetime.PauseProcessingMessagesAsync(jobId, TimeSpan.FromSeconds(5), CancellationToken.None);

            // Act
            await producer.ProduceAsync(message);

            // Assert
            OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId, TimeSpan.FromSeconds(10));
            AssertReceivedOrderEventDataForW3C(message, eventData);
        }
    }

    public class TestUnavailableDependencyAzureServiceBusMessageHandler : CircuitBreakerServiceBusMessageHandler<Order>
    {
        public const int RequiredAttempts = 3;

        private readonly OrderMessageSink _messageSink;

        public TestUnavailableDependencyAzureServiceBusMessageHandler(
            OrderMessageSink messageSink,
            IMessagePumpCircuitBreaker circuitBreaker,
            ILogger<CircuitBreakerServiceBusMessageHandler<Order>> logger) : base(circuitBreaker, logger)
        {
            _messageSink = messageSink;
        }

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
            _messageSink.SendOrder(message);

            if (_messageSink.ReceiveOrders().Length < RequiredAttempts)
            {
                throw new InvalidOperationException(
                    $"Simulate an unhealthy dependency system! (DeliveryCount: {messageContext.DeliveryCount})");
            }

            return Task.CompletedTask;
        }
    }

    public class OrderMessageSink
    {
        private readonly Collection<Order> _orders = new();

        public void SendOrder(Order message)
        {
            _orders.Add(message);
        }

        public Order[] ReceiveOrders() => _orders.ToArray();

        public async Task ShouldReceiveOrdersDuringBreakAsync(string messageId)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts, _orders.Count(o => o.Id == messageId));

            }).Every(_1s).Timeout(_1min).FailWith($"message should be retried {RequiredAttempts} times with the help of the circuit breaker");
        }

        public async Task ShouldReceiveOrdersAfterBreakAsync(string messageId)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts + 1, _orders.Count);
                Assert.Single(_orders.Where(o => o.Id == messageId));

            }).Every(_1s).Timeout(_1min).FailWith("pump should continue normal message processing after the message was retried");
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
}
