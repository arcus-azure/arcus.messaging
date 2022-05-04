using System;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    /// <summary>
    /// Represents an event consumer which receives events from an Azure Service Bus.
    /// </summary>
    public class TestServiceBusMessageEventConsumer : IAsyncDisposable
    {
        private readonly ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        private TestServiceBusMessageEventConsumer(ServiceBusEventConsumerHost consumerHost)
        {
            Guard.NotNull(consumerHost, nameof(consumerHost), "Requires an Azure Service Bus consumer host instance to consume messages");
            _serviceBusEventConsumerHost = consumerHost;
        }

        /// <summary>
        /// Starts an new event consumer which receives events from an Azure Service Bus entity.
        /// </summary>
        /// <param name="configuration">The test configuration to retrieve the Azure Service Bus test infrastructure.</param>
        /// <param name="logger">The logger to write diagnostic messages during consuming the messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static async Task<TestServiceBusMessageEventConsumer> StartNewAsync(TestConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the Azure Service Bus test infrastructure");

            logger = logger ?? NullLogger.Instance;

            var topicName = configuration.GetValue<string>("Arcus:Infra:ServiceBus:TopicName");
            var connectionString = configuration.GetValue<string>("Arcus:Infra:ServiceBus:ConnectionString");
            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);

            var serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, logger);
            return new TestServiceBusMessageEventConsumer(serviceBusEventConsumerHost);
        }

        /// <summary>
        /// Receives an event produced on the Azure Service Bus.
        /// </summary>
        /// <param name="eventId">The ID to identity the produced event.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventId"/> is blank.</exception>
        public OrderCreatedEventData ConsumeOrderEvent(string eventId)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "Requires a non-blank event ID to identity the produced event on the Azure Service Bus");

            string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(eventId, retryCount: 10);
            Assert.NotEmpty(receivedEvent);

            EventBatch<Event> eventBatch = EventParser.Parse(receivedEvent);
            Assert.NotNull(eventBatch);
            Event @event = Assert.Single(eventBatch.Events);
            Assert.NotNull(@event);

            var data = @event.Data.ToString();
            Assert.NotNull(data);

            var eventData = JsonConvert.DeserializeObject<OrderCreatedEventData>(data, new MessageCorrelationInfoJsonConverter());
            return eventData;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _serviceBusEventConsumerHost.StopAsync();
        }
    }
}