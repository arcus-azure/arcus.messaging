using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Arcus.Messaging.Tests.Integration.MessagePump.EventHubs
{
    public class TestEventHubsMessageProducer
    {
        private readonly string _connectionString;
        private readonly string _eventHubName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessageProducer" /> class.
        /// </summary>
        public TestEventHubsMessageProducer(string connectionString, string eventHubName)
        {
            _connectionString = connectionString;
            _eventHubName = eventHubName;
        }

        public async Task ProduceAsync(EventData eventData)
        {
            await using (var client = new EventHubProducerClient(_connectionString, _eventHubName))
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
