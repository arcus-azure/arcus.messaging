using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v2;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Represents a test Azure Service Bus message handler to process <see cref="OrderV2"/> messages.
    /// </summary>
    public class OrderV2AzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<OrderV2>
    {
        private readonly ConcurrentQueue<OrderV2> _orders = new ConcurrentQueue<OrderV2>();
        
        /// <summary>
        /// Gets the flag indicating whether or not this message handler was being used to process a message.
        /// </summary>
        public bool IsProcessed { get; private set; }

        /// <summary>
        /// Gets the current processed messages.
        /// </summary>
        public OrderV2[] ProcessedMessages => _orders.ToArray();
        
        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task ProcessMessageAsync(
            OrderV2 message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IsProcessed = true;
            _orders.Enqueue(message);
            
            return Task.CompletedTask;
        }
    }
}
