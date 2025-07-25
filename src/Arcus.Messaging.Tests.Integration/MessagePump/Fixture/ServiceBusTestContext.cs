﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Testing;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    /// <summary>
    /// Represents test-friendly interaction with Azure Service Bus message pumps to verify message routing and processing.
    /// </summary>
    internal class ServiceBusTestContext : IAsyncDisposable
    {
        private readonly TemporaryServiceBusEntityState _serviceBus;
        private readonly ServiceBusConfig _serviceBusConfig;
        private readonly List<IAsyncDisposable> _disposables = [];
        private readonly Collection<string> _subscriptionNames = [];
        private bool _isStarted;
        private readonly ILogger _logger;

        private ServiceBusTestContext(TemporaryServiceBusEntityState serviceBus, ILogger logger)
        {
            _serviceBus = serviceBus;
            _serviceBusConfig = serviceBus.ServiceBusConfig;
            _logger = logger;

            Services.AddTestLogging(logger);
        }

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
        internal ServiceBusMessageHandlerCollection WhenServiceBusQueueMessagePump(Action<AzureServiceBusMessagePumpOptions> configureOptions = null)
        {
            return Services.AddServiceBusQueueMessagePump(_serviceBus.QueueName, _serviceBusConfig.HostName, new DefaultAzureCredential(), configureOptions);
        }

        /// <summary>
        /// Registers an Azure Service Bus message pump listening on a topic subscription.
        /// </summary>
        internal ServiceBusMessageHandlerCollection WhenServiceBusTopicMessagePump(Action<AzureServiceBusMessagePumpOptions> configureOptions = null)
        {
            string subscriptionName = $"test-{Guid.NewGuid()}";
            _subscriptionNames.Add(subscriptionName);

            return Services.AddServiceBusTopicMessagePump(_serviceBus.TopicName, subscriptionName, _serviceBusConfig.HostName, new DefaultAzureCredential(), configureOptions);
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
                    _disposables.Add(await TemporaryTopicSubscription.CreateIfNotExistsAsync(_serviceBusConfig.GetAdminClient(), _serviceBus.TopicName, subscriptionName, _logger));
                }

                _disposables.Insert(0, await Worker.StartNewAsync(Services));
                _isStarted = true;
            }

            var messages = configureMessages.DefaultIfEmpty(_ => { }).Select(configureMessage =>
            {
                var builder = new ServiceBusMessageBuilder();
                configureMessage?.Invoke(builder);

                return builder.Build();
            }).ToArray();

            string entityName = _subscriptionNames.Any()
                ? _serviceBus.TopicName
                : _serviceBus.QueueName;

            var producer = TestServiceBusMessageProducer.CreateFor(entityName, _serviceBusConfig, _logger);
            await producer.ProduceAsync(messages);

            return messages;
        }

        /// <summary>
        /// Verifies that the given <paramref name="messages"/> are consumed by the Azure Service Bus message pump via a matched handler.
        /// </summary>
        internal async Task ShouldConsumeViaMatchedHandlerAsync(IEnumerable<ServiceBusMessage> messages)
        {
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
            foreach (var message in messages)
            {
                await Poll.Target(() => _serviceBus.Queue.Messages.Where(msg => msg.MessageId == message.MessageId).ToListAsync())
                          .Until(available => available.Count is 0)
                          .FailWith($"Azure Service bus message '{message.MessageId}' did not get completed in time");
            }
        }

        /// <summary>
        /// Verifies that the given <paramref name="messages"/> are not consumed by the Azure Service Bus message pump, but dead-lettered.
        /// </summary>
        internal async Task ShouldNotConsumeButDeadLetterAsync(IEnumerable<ServiceBusMessage> messages)
        {
            foreach (var message in messages)
            {
                await Poll.Target(() => _serviceBus.Queue.Messages.FromDeadLetter().Where(msg => msg.MessageId == message.MessageId).ToListAsync())
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
            foreach (var message in messages)
            {
                await Poll.Target(() => _serviceBus.Queue.Messages.Where(msg => msg.MessageId == message.MessageId && msg.DeliveryCount > 0).ToListAsync())
                          .Until(abandoned => abandoned.Count > 0)
                          .Every(TimeSpan.FromMilliseconds(100))
                          .Timeout(TimeSpan.FromMinutes(2))
                          .FailWith($"cannot receive abandoned message with the message ID: '{message.MessageId}' in time");
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
            private Encoding _encoding = Encoding.UTF8;
            private TraceParent _traceParent = TraceParent.Generate();
            private readonly Dictionary<string, object> _applicationProperties = new();
            private readonly Collection<Action<Order>> _bodyConfigurations = new();

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
                    MessageId = order.Id,
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
            await using var disposables = new DisposableCollection(_logger);
            disposables.AddRange(_disposables);
        }
    }
}
