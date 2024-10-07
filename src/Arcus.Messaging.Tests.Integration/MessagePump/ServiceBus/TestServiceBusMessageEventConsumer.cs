using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Workers.ServiceBus.Fixture;
using Arcus.Testing;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using TestConfig = Arcus.Messaging.Tests.Integration.Fixture.TestConfig;

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
        public OrderCreatedEventData ConsumeOrderEventForHierarchical(string eventId)
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
        /// Receives an event produced on the Azure Service Bus.
        /// </summary>
        /// <param name="transactionId">The ID to identity the produced event.</param>
        /// <param name="timeoutInSeconds">The optional time-out in seconds for the event to be arrived.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transactionId"/> is blank.</exception>
        [Obsolete("Use the " + nameof(ConsumeOrderEventForW3CAsync) + " instead")]
        public OrderCreatedEventData ConsumeOrderEventForW3C(string transactionId, int timeoutInSeconds = 60)
        {
            Guard.NotNullOrWhitespace(transactionId, nameof(transactionId), "Requires a non-blank transaction ID to identity the produced event on the Azure Service Bus");
            Guard.NotLessThan(timeoutInSeconds, 0, nameof(timeoutInSeconds), "Requires a time-out in seconds of at least 1 second");

            // TODO: will be simplified, once all the message handlers are using the same event publishing (https://github.com/arcus-azure/arcus.messaging/issues/343).
            CloudEvent receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent((CloudEvent ev) =>
            {
                var data = ev.Data.ToString();
                var eventData = JsonConvert.DeserializeObject<OrderCreatedEventData>(data, new MessageCorrelationInfoJsonConverter());

                return eventData.CorrelationInfo.TransactionId == transactionId;
            }, timeout: TimeSpan.FromSeconds(timeoutInSeconds));
            
            var data = receivedEvent.Data.ToString();

            var eventData = JsonConvert.DeserializeObject<OrderCreatedEventData>(data, new MessageCorrelationInfoJsonConverter());
            return eventData;
        }

        /// <summary>
        /// Receives an event produced on the Azure Service Bus.
        /// </summary>
        /// <param name="transactionId">The ID to identity the produced event.</param>
        public async Task<OrderCreatedEventData> ConsumeOrderEventForW3CAsync(string transactionId)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            
            FileInfo[] foundFiles =
                await Poll.Target(() => Task.FromResult(directory.GetFiles(transactionId + ".json", SearchOption.AllDirectories)))
                          .Until(files => files.Length > 0 && files.All(f => f.Length > 0))
                          .Every(TimeSpan.FromSeconds(0.5))
                          .Timeout(TimeSpan.FromMinutes(2))
                          .FailWith("Failed to retrieve the necessary produced message");

            FileInfo foundFile = Assert.Single(foundFiles);
            string json = await File.ReadAllTextAsync(foundFile.FullName);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new MessageCorrelationInfoJsonConverter());

            return JsonConvert.DeserializeObject<OrderCreatedEventData>(json, settings);
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