using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Testing;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using ServiceBusEntityType = Arcus.Messaging.Abstractions.ServiceBus.ServiceBusEntityType;
using ServiceBusMessagePumpOptions = Arcus.Messaging.Pumps.ServiceBus.Configuration.ServiceBusMessagePumpOptions;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    /// <summary>
    /// Represents test-friendly interaction with Azure Service Bus message pumps to verify message routing and processing.
    /// </summary>
    internal class ServiceBusTestContext : IAsyncDisposable
    {
        private enum ServiceBusOperation { TriggerRun, ClientCreation }
        private readonly Collection<(DateTimeOffset time, ServiceBusOperation type)> _timedOperations = [];

        private readonly TemporaryServiceBusEntityState _serviceBus;
        private readonly ServiceBusConfig _serviceBusConfig;
        private readonly List<IAsyncDisposable> _disposables = [];
        private readonly Collection<string> _subscriptionNames = [];
        private bool _isStarted;
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        private ServiceBusTestContext(TemporaryServiceBusEntityState serviceBus, ILogger logger)
        {
            _serviceBus = serviceBus;
            _serviceBusConfig = serviceBus.ServiceBusConfig;
            _logger = logger;

            Services.AddTestLogging(logger);
        }

        private TemporaryQueue Queue => UseSessions ? _serviceBus.QueueWithSession : _serviceBus.Queue;
        private TemporaryTopic Topic => _serviceBus.Topic;
        private bool UseTrigger { get; } = Bogus.Random.Bool();

        /// <summary>
        /// Gets or sets a value indicating whether the Azure Service Bus message pump managed by the test context should use sessions.
        /// </summary>
        internal bool UseSessions { get; set; } = Bogus.Random.Bool();

        /// <summary>
        /// Gets the collection of services that are used to configure the Azure Service Bus message pump.
        /// </summary>
        internal WorkerOptions Services { get; } = [];

        /// <summary>
        /// Creates a new <see cref="ServiceBusTestContext"/> for the given <paramref name="serviceBusEntityState"/>.
        /// </summary>
        /// <returns></returns>
        internal static ServiceBusTestContext GivenServiceBus(TemporaryServiceBusEntityState serviceBusEntityState, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(serviceBusEntityState);
            logger ??= NullLogger.Instance;

            return new ServiceBusTestContext(serviceBusEntityState, logger);
        }

        /// <summary>
        /// Registers an Azure Service Bus message pump for the given <paramref name="entityType"/>.
        /// </summary>
        internal ServiceBusMessageHandlerCollection WhenServiceBusMessagePump(ServiceBusEntityType entityType)
        {
            return entityType switch
            {
                ServiceBusEntityType.Queue => WhenServiceBusQueueMessagePump(),
                ServiceBusEntityType.Topic => WhenServiceBusTopicMessagePump(),
                _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Azure Service Bus entity type"),
            };
        }

        /// <summary>
        /// Registers an Azure Service Bus message pump listening on a queue.
        /// </summary>
        internal ServiceBusMessageHandlerCollection WhenServiceBusQueueMessagePump(Action<ServiceBusMessagePumpOptions> configureOptions = null)
        {
            return WhenOnlyServiceBusQueueMessagePump(configureOptions)
                   .WithUnrelatedServiceBusMessageHandler()
                   .WithUnrelatedServiceBusMessageHandler();
        }

        internal ServiceBusMessageHandlerCollection WhenOnlyServiceBusQueueMessagePump(Action<ServiceBusMessagePumpOptions> configureOptions = null)
        {
            string sessionAwareDescription = UseSessions ? " session-aware" : string.Empty;
            _logger.LogTrace("[Test:Setup] Register Azure Service Bus{SessionDescription} queue message pump", sessionAwareDescription);

            return UseTrigger
                ? Services.AddServiceBusQueueMessagePump(Queue.Name, _ => CreateServiceBusClient(), ConfigureWithTrigger)
                : Services.AddServiceBusQueueMessagePump(Queue.Name, _serviceBusConfig.HostName, new DefaultAzureCredential(), ConfigureWithoutTrigger);

            void ConfigureWithoutTrigger(ServiceBusMessagePumpOptions options)
            {
                if (UseSessions)
                {
                    options.UseSessions();
                }

                configureOptions?.Invoke(options);
            }

            void ConfigureWithTrigger(ServiceBusMessagePumpOptions options)
            {
                ConfigureWithoutTrigger(options);
                AddServiceBusTrigger(options);
            }
        }

        /// <summary>
        /// Registers an Azure Service Bus message pump listening on a topic subscription.
        /// </summary>
        internal ServiceBusMessageHandlerCollection WhenServiceBusTopicMessagePump(Action<ServiceBusMessagePumpOptions> configureOptions = null)
        {
            string subscriptionName = $"test-{Guid.NewGuid()}";
            _subscriptionNames.Add(subscriptionName);

            string sessionAwareDescription = UseSessions ? " session-aware" : string.Empty;
            _logger.LogTrace("[Test:Setup] Register Azure Service Bus{SessionDescription} topic message pump", sessionAwareDescription);

            var collection = UseTrigger
                ? Services.AddServiceBusTopicMessagePump(Topic.Name, subscriptionName, _ => CreateServiceBusClient(), ConfigureWithTrigger)
                : Services.AddServiceBusTopicMessagePump(Topic.Name, subscriptionName, _serviceBusConfig.HostName, new DefaultAzureCredential(), ConfigureWithoutTrigger);

            return collection.WithUnrelatedServiceBusMessageHandler()
                             .WithUnrelatedServiceBusMessageHandler();

            void ConfigureWithoutTrigger(ServiceBusMessagePumpOptions options)
            {
                if (UseSessions)
                {
                    options.UseSessions(sessions => sessions.SessionIdleTimeout = TimeSpan.FromSeconds(1));
                }

                configureOptions?.Invoke(options);
            }

            void ConfigureWithTrigger(ServiceBusMessagePumpOptions options)
            {
                ConfigureWithoutTrigger(options);
                AddServiceBusTrigger(options);
            }
        }

        private ServiceBusClient CreateServiceBusClient()
        {
            _timedOperations.Add((DateTimeOffset.UtcNow, ServiceBusOperation.ClientCreation));
            return new ServiceBusClient(_serviceBusConfig.HostName, new DefaultAzureCredential());
        }

        private void AddServiceBusTrigger(ServiceBusMessagePumpOptions options)
        {
            options.Hooks.BeforeStartup(_ =>
            {
                _timedOperations.Add((DateTimeOffset.UtcNow, ServiceBusOperation.TriggerRun));
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Places a single message on the Azure Service Bus queue or topic subscription, depending on the configured message pump.
        /// </summary>
        internal async Task<ServiceBusMessage> WhenProducingMessageAsync(Action<ServiceBusMessageBuilder> configureMessage = null)
        {
            ServiceBusMessage[] messages = await WhenProducingMessagesAsync(configureMessage);
            return Assert.Single(messages);
        }

        /// <summary>
        /// Places a number of messages on the Azure Service Bus queue or topic subscription, depending on the configured message pump.
        /// </summary>
        internal async Task<ServiceBusMessage[]> WhenProducingMessagesAsync(int amountOfMessages)
        {
            return await WhenProducingMessagesAsync(Enumerable.Repeat<Action<ServiceBusMessageBuilder>>(null, amountOfMessages).ToArray());
        }

        /// <summary>
        /// Places a number of messages on the Azure Service Bus queue or topic subscription, depending on the configured message pump.
        /// </summary>
        /// <param name="configureMessages">
        ///     The actions to configure each message separately, each action represents a single message.
        ///     A single message gets produced when no actions are provided.
        /// </param>
        internal async Task<ServiceBusMessage[]> WhenProducingMessagesAsync(params Action<ServiceBusMessageBuilder>[] configureMessages)
        {
            if (!_isStarted)
            {
                foreach (var subscriptionName in _subscriptionNames)
                {
                    _disposables.Add(await CreateTopicSubscriptionAsync(subscriptionName));
                }

                _logger.LogTrace("--------------------------------------------------------------------------------------------------------");

                _disposables.Insert(0, await Worker.StartNewAsync(Services));
                _isStarted = true;
            }

            ServiceBusMessage[] messages = CreateMessages(configureMessages);
            string entityName = _subscriptionNames.Count > 0 ? Topic.Name : Queue.Name;

            var producer = TestServiceBusMessageProducer.CreateFor(entityName, _serviceBusConfig, _logger);
            await producer.ProduceAsync(messages);

            return messages;
        }

        private async Task<TemporaryTopicSubscription> CreateTopicSubscriptionAsync(string subscriptionName)
        {
            return await TemporaryTopicSubscription.CreateIfNotExistsAsync(
                _serviceBusConfig.GetAdminClient(),
                Topic.Name,
                subscriptionName,
                _logger,
                options => options.OnSetup.CreateSubscriptionWith(sub =>
                {
                    sub.RequiresSession = UseSessions;
                }));
        }

        private ServiceBusMessage[] CreateMessages(Action<ServiceBusMessageBuilder>[] configureMessages)
        {
            string sessionId = Bogus.Random.Guid().ToString();
            return configureMessages.DefaultIfEmpty(_ => { }).Select(configureMessage =>
            {
                var builder = new ServiceBusMessageBuilder();
                if (UseSessions)
                {
                    string sameOrNewSessionId = Bogus.Random.Bool(0.8f)
                        ? sessionId
                        : Bogus.Random.Guid().ToString();

                    builder.WithSessionId(sameOrNewSessionId);
                }

                configureMessage?.Invoke(builder);

                return builder.Build();

            }).ToArray();
        }

        /// <summary>
        /// Verifies that the given <paramref name="messages"/> are consumed by the Azure Service Bus message pump via a matched handler.
        /// </summary>
        internal async Task ShouldConsumeViaMatchedHandlerAsync(IEnumerable<ServiceBusMessage> messages)
        {
            AssertTimedOperations();
            foreach (var message in messages)
            {
                OrderCreatedEventData eventData = await DiskMessageEventConsumer.ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData);
            }
        }

        /// <summary>
        /// Verifies that the given <paramref name="messages"/> are completed.
        /// </summary>
        internal async Task ShouldCompleteConsumedAsync(IEnumerable<ServiceBusMessage> messages)
        {
            AssertTimedOperations();
            foreach (var message in messages)
            {
                await Poll.Target(() => Queue.Messages.Where(msg => msg.MessageId == message.MessageId).ToListAsync())
                          .Until(available => available.Count is 0)
                          .FailWith($"Azure Service bus message '{message.MessageId}' did not get completed in time");
            }
        }

        /// <summary>
        /// Verifies that the given <paramref name="messages"/> are not consumed by the Azure Service Bus message pump, but dead-lettered.
        /// </summary>
        internal async Task ShouldNotConsumeButDeadLetterAsync(IEnumerable<ServiceBusMessage> messages)
        {
            AssertTimedOperations();
            foreach (var message in messages)
            {
                await Poll.Target(() => Queue.Messages.FromDeadLetter().Where(msg => msg.MessageId == message.MessageId).ToListAsync())
                          .Until(deadLettered => deadLettered.Count > 0)
                          .Every(TimeSpan.FromMilliseconds(500))
                          .Timeout(TimeSpan.FromMinutes(2))
                          .FailWith($"cannot receive dead-lettered message with message ID: '{message.MessageId}' in time");
            }
        }

        /// <summary>
        /// Verifies that the given <paramref name="messages"/> are not consumed by the Azure Service Bus message pump, but abandoned.
        /// </summary>
        internal async Task ShouldNotConsumeButAbandonAsync(IEnumerable<ServiceBusMessage> messages)
        {
            AssertTimedOperations();
            foreach (var message in messages)
            {
                await Poll.Target(() => Queue.Messages.Where(msg => msg.MessageId == message.MessageId && msg.DeliveryCount > 0).ToListAsync())
                          .Until(abandoned => abandoned.Count > 0)
                          .Every(TimeSpan.FromMilliseconds(100))
                          .Timeout(TimeSpan.FromMinutes(2))
                          .FailWith($"cannot receive abandoned message with the message ID: '{message.MessageId}' in time");
            }
        }

        private void AssertTimedOperations()
        {
            if (UseTrigger)
            {
                Assert.Collection(_timedOperations,
                       op => Assert.Equal(ServiceBusOperation.TriggerRun, op.type),
                       op => Assert.Equal(ServiceBusOperation.ClientCreation, op.type));

                DateTimeOffset[] times = _timedOperations.Select(op => op.time).ToArray();
                Assert.True(times.Order().SequenceEqual(times),
                    $"Service Bus operations should be run in the expected order, but weren't: {string.Join(", ", times.Select(t => t.ToString("s")))}");
            }
        }

        private static void AssertReceivedOrderEventDataForW3C(
            ServiceBusMessage message,
            OrderCreatedEventData receivedEventData)
        {
            var encoding = Encoding.GetEncoding(message.ApplicationProperties[PropertyNames.Encoding].ToString() ?? Encoding.UTF8.WebName);
            string json = encoding.GetString(message.Body);

            var order = JsonConvert.DeserializeObject<Order>(json);

            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();
            Assert.NotNull(receivedEventData);
            Assert.NotNull(receivedEventData.CorrelationInfo);
            Assert.Equal(order.Id, receivedEventData.Id);
            Assert.Equal(order.Amount, receivedEventData.Amount);
            Assert.Equal(order.ArticleNumber, receivedEventData.ArticleNumber);
            Assert.Equal(transactionId, receivedEventData.CorrelationInfo.TransactionId);
            Assert.NotNull(receivedEventData.CorrelationInfo.OperationId);
            Assert.Equal(operationParentId, receivedEventData.CorrelationInfo.OperationParentId);
        }

        internal sealed class ServiceBusMessageBuilder
        {
            private string _messageId, _sessionId;
            private Encoding _encoding = Encoding.UTF8;
            private TraceParent _traceParent = TraceParent.Generate();
            private readonly Dictionary<string, object> _applicationProperties = new();
            private readonly Collection<Action<Order>> _bodyConfigurations = new();

            internal ServiceBusMessageBuilder WithSessionId(string sessionId)
            {
                _sessionId = sessionId;
                return this;
            }

            internal ServiceBusMessageBuilder WithMessageId(string messageId)
            {
                _messageId = messageId;
                return this;
            }

            /// <summary>
            /// Configures the encoding to use for the message body.
            /// </summary>
            internal ServiceBusMessageBuilder WithEncoding(Encoding encoding)
            {
                _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding), "The encoding cannot be null");
                return this;
            }

            /// <summary>
            /// Adds an application property to the message.
            /// </summary>
            internal ServiceBusMessageBuilder WithApplicationProperty(KeyValuePair<string, object> property)
            {
                return WithApplicationProperty(property.Key, property.Value);
            }

            /// <summary>
            /// Adds an application property to the message.
            /// </summary>
            internal ServiceBusMessageBuilder WithApplicationProperty(string key, object value)
            {
                _applicationProperties[key] = value ?? throw new ArgumentNullException(nameof(value), "The application property value cannot be null");
                return this;
            }

            /// <summary>
            /// Removes the trace parent from the message, meaning that no W3C trace context will be included in the message.
            /// </summary>
            internal ServiceBusMessageBuilder WithoutTraceParent()
            {
                _traceParent = null;
                return this;
            }

            internal ServiceBusMessageBuilder WithBody(Action<Order> configureBody)
            {
                _bodyConfigurations.Add(configureBody);
                return this;
            }

            internal ServiceBusMessage Build()
            {
                Order order = OrderGenerator.Generate();
                Assert.All(_bodyConfigurations, configureBody => configureBody(order));

                string json = JsonConvert.SerializeObject(order);
                byte[] raw = _encoding.GetBytes(json);

                var message = new ServiceBusMessage(raw)
                {
                    MessageId = _messageId ?? order.Id,
                    SessionId = _sessionId,
                    ApplicationProperties =
                    {
                        { "Content-Type", "application/json" },
                        { PropertyNames.Encoding, _encoding.WebName },
                    }
                };

                if (_traceParent != null)
                {
                    message.ApplicationProperties["Diagnostic-Id"] = _traceParent.DiagnosticId;
                }

                Assert.All(_applicationProperties, prop => message.ApplicationProperties.Add(prop.Key, prop.Value));
                return message;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("--------------------------------------------------------------------------------------------------------");

            await using var disposables = new DisposableCollection(_logger);
            disposables.AddRange(_disposables);
        }
    }
}
