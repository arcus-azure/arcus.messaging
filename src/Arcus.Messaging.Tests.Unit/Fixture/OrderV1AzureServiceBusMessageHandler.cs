using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Represents a test Azure Service Bus message handler to process <see cref="Core.Messages.v1.Order"/> messages.
    /// </summary>
    public class OrderV1AzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Core.Messages.v1.Order>
    {
        private readonly ConcurrentQueue<Core.Messages.v1.Order> _orders = new ConcurrentQueue<Core.Messages.v1.Order>();
        
        /// <summary>
        /// Gets the flag indicating whether or not this message handler was being used to process a message.
        /// </summary>
        public bool IsProcessed { get; private set; }

        /// <summary>
        /// Gets the current of processed messages.
        /// </summary>
        public Core.Messages.v1.Order[] ProcessedMessages => _orders.ToArray();
        
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
            Core.Messages.v1.Order message,
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
