using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Arcus.Messaging.Tests.Unit.EventHubs.Fixture
{
   /// <summary>
    /// Represents an <see cref="EventHubProducerClient"/> implementation that holds the send messages in-memory.
    /// </summary>
    public class InMemoryEventHubProducerClient : EventHubProducerClient
    {
        private readonly ConcurrentQueue<EventData> _messages = new ConcurrentQueue<EventData>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventHubProducerClient" /> class.
        /// </summary>
        public InMemoryEventHubProducerClient()
            : base("Endpoint=sb://arcus-messaging-in-memory.servicebus.windows.net/;SharedAccessKeyName=Name;SharedAccessKey=Key",
                   "eventhubs-name")
        {
        }

        /// <summary>
        /// Gets the currently send <see cref="EventData"/> messages.
        /// </summary>
        public EventData[] Messages => _messages.ToArray();

        /// <summary>
        ///   Sends a set of events to the associated Event Hub as a single operation.  To avoid the
        ///   overhead associated with measuring and validating the size in the client, validation will
        ///   be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///   The call will fail if the size of the specified set of events exceeds the maximum allowable
        ///   size of a single batch.
        /// </summary>
        /// <param name="eventBatch">The set of event data to send.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>
        ///   A task to be resolved on when the operation has completed; if no exception is thrown when awaited, the
        ///   Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to
        ///   its partition.
        /// </returns>
        /// <remarks>
        ///   When published, the result is atomic; either all events that belong to the set were successful or all
        ///   have failed.  Partial success is not possible.
        /// </remarks>
        /// <exception cref="T:Azure.Messaging.EventHubs.EventHubsException">
        ///   Occurs when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.  The <see cref="P:Azure.Messaging.EventHubs.EventHubsException.Reason" /> will be set to
        ///   <see cref="F:Azure.Messaging.EventHubs.EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">
        ///   Occurs when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="P:Azure.Messaging.EventHubs.EventData.Properties" /> collection that is an
        ///   unsupported type for serialization.  See the <see cref="P:Azure.Messaging.EventHubs.EventData.Properties" /> remarks for details.
        /// </exception>
        /// <seealso cref="M:Azure.Messaging.EventHubs.Producer.EventHubProducerClient.SendAsync(System.Collections.Generic.IEnumerable{Azure.Messaging.EventHubs.EventData},Azure.Messaging.EventHubs.Producer.SendEventOptions,System.Threading.CancellationToken)" />
        /// <seealso cref="M:Azure.Messaging.EventHubs.Producer.EventHubProducerClient.SendAsync(Azure.Messaging.EventHubs.Producer.EventDataBatch,System.Threading.CancellationToken)" />
        /// <seealso cref="M:Azure.Messaging.EventHubs.Producer.EventHubProducerClient.CreateBatchAsync(System.Threading.CancellationToken)" />
        public override async Task SendAsync(
            IEnumerable<EventData> eventBatch, 
            CancellationToken cancellationToken = new CancellationToken())
        {
            await SendAsync(eventBatch, options: null, cancellationToken);
        }

        /// <summary>
        ///   Sends a set of events to the associated Event Hub as a single operation.  To avoid the
        ///   overhead associated with measuring and validating the size in the client, validation will
        ///   be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///   The call will fail if the size of the specified set of events exceeds the maximum allowable
        ///   size of a single batch.
        /// </summary>
        /// <param name="eventBatch">The set of event data to send.</param>
        /// <param name="options">The set of options to consider when sending this batch.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>
        ///   A task to be resolved on when the operation has completed; if no exception is thrown when awaited, the
        ///   Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to
        ///   its partition.
        /// </returns>
        /// <remarks>
        ///   When published, the result is atomic; either all events that belong to the set were successful or all
        ///   have failed.  Partial success is not possible.
        /// </remarks>
        /// <exception cref="T:System.InvalidOperationException">Occurs when both a partition identifier and partition key have been specified in the <paramref name="options" />.</exception>
        /// <exception cref="T:Azure.Messaging.EventHubs.EventHubsException">
        ///   Occurs when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.  The <see cref="P:Azure.Messaging.EventHubs.EventHubsException.Reason" /> will be set to
        ///   <see cref="F:Azure.Messaging.EventHubs.EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">
        ///   Occurs when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="P:Azure.Messaging.EventHubs.EventData.Properties" /> collection that is an
        ///   unsupported type for serialization.  See the <see cref="P:Azure.Messaging.EventHubs.EventData.Properties" /> remarks for details.
        /// </exception>
        /// <seealso cref="M:Azure.Messaging.EventHubs.Producer.EventHubProducerClient.SendAsync(System.Collections.Generic.IEnumerable{Azure.Messaging.EventHubs.EventData},System.Threading.CancellationToken)" />
        /// <seealso cref="M:Azure.Messaging.EventHubs.Producer.EventHubProducerClient.SendAsync(Azure.Messaging.EventHubs.Producer.EventDataBatch,System.Threading.CancellationToken)" />
        /// <seealso cref="M:Azure.Messaging.EventHubs.Producer.EventHubProducerClient.CreateBatchAsync(Azure.Messaging.EventHubs.Producer.CreateBatchOptions,System.Threading.CancellationToken)" />
        public override Task SendAsync(
            IEnumerable<EventData> eventBatch,
            SendEventOptions options,
            CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (EventData eventData in eventBatch)
            {
                _messages.Enqueue(eventData);
            }

            return Task.CompletedTask;
        }
    }
}
