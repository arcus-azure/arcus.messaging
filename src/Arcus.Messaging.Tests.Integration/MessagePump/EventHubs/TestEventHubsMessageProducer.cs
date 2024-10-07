using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
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
        private readonly string _name;
        private readonly EventHubsConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessageProducer" /> class.
        /// </summary>
        public TestEventHubsMessageProducer(string name, EventHubsConfig config)
        {
            _name = name;
            _config = config;
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

            await using (EventHubProducerClient client = _config.GetProducerClient(_name))
            {
                await client.SendAsync(new[] { eventData });
            }
        }
    }
}
