using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GuardNet;

namespace Arcus.Messaging.Tests.Integration.MessagePump.EventHubs
{
    /// <summary>
    /// Represents an Azure EventHubs message producer that places event messages on a configured Azure EventHubs.
    /// </summary>
    public class TestEventHubsMessageProducer
    {
        private readonly string _eventHubsConnectionString;
        private readonly string _eventHubName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessageProducer" /> class.
        /// </summary>
        /// <param name="eventHubsConnectionString">The connection string to access the Azure EventHubs.</param>
        /// <param name="eventHubName">The name of the Azure EventHubs where an event message should be placed.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventHubsConnectionString"/> or the <paramref name="eventHubName"/> is blank.</exception>
        public TestEventHubsMessageProducer(string eventHubsConnectionString, string eventHubName)
        {
            Guard.NotNullOrWhitespace(eventHubsConnectionString, nameof(eventHubsConnectionString), "Requires a non-blank connection string to access the Azure EventHubs");
            Guard.NotNullOrWhitespace(eventHubName, nameof(eventHubName), "Requires a non-blank name of the Azure EventHubs");
            
            _eventHubsConnectionString = eventHubsConnectionString;
            _eventHubName = eventHubName;
        }

        /// <summary>
        /// Places an event message on the configured Azure EventHubs.
        /// </summary>
        /// <param name="eventData">The event message to place on the Azure EventHubs.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventData"/> is <c>null</c>.</exception>
        /// <exception cref="SerializationException">
        ///   Thrown when the <paramref name="eventData" /> has a member in its <see cref="EventData.Properties" /> collection that is an
        ///   unsupported type for serialization. See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public async Task ProduceAsync(EventData eventData)
        {
            Guard.NotNull(eventData, nameof(eventData), "Requires an event data instance to place on the configured Azure EventHubs");

            await using (var client = new EventHubProducerClient(_eventHubsConnectionString, _eventHubName))
            {
                using (EventDataBatch eventBatch = await client.CreateBatchAsync())
                {
                    eventBatch.TryAdd(eventData);
                    await client.SendAsync(eventBatch);
                }
            }
        }
    }
}
