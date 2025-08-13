using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrderBatchMessageHandler : IServiceBusMessageHandler<OrderBatch>
    {
        private readonly IServiceBusMessageHandler<Order> _messageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBatchMessageHandler"/> class.
        /// </summary>
        public OrderBatchMessageHandler(
            ILogger<WriteOrderToDiskAzureServiceBusMessageHandler> logger)
        {
            _messageHandler = new WriteOrderToDiskAzureServiceBusMessageHandler(logger);
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="batch">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            OrderBatch batch,
            ServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            foreach (Order order in batch.Orders)
            {
                await _messageHandler.ProcessMessageAsync(order, messageContext, correlationInfo, cancellationToken);
            }
        }
    }
}
