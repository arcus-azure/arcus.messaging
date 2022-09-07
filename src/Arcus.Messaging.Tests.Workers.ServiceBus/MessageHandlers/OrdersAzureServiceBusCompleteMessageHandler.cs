using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersAzureServiceBusCompleteMessageHandler : AzureServiceBusMessageHandler<Order>
    {
        private readonly IAzureServiceBusMessageHandler<Order> _orderMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersAzureServiceBusCompleteMessageHandler"/> class.
        /// </summary>
        public OrdersAzureServiceBusCompleteMessageHandler(
            IEventGridPublisher eventGridPublisher, 
            IMessageCorrelationInfoAccessor correlationAccessor,
            ILogger<OrdersAzureServiceBusMessageHandler> logger) 
            : base(logger)
        {
            _orderMessageHandler = new OrdersAzureServiceBusMessageHandler(eventGridPublisher, correlationAccessor, logger);
        }

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
        public override async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await _orderMessageHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
            await CompleteMessageAsync();
        }
    }
}
