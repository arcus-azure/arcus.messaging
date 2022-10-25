using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersAzureServiceBusAbandonMessageHandler : AzureServiceBusMessageHandler<Order>
    {
        private readonly IEventGridPublisher _eventGridPublisher;

        private static int _deliveryCount;

        public OrdersAzureServiceBusAbandonMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrdersAzureServiceBusAbandonMessageHandler> logger)
            : base(logger)
        {
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));

            _eventGridPublisher = eventGridPublisher;
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="order">Message that was received</param>
        /// <param name="azureMessageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public override async Task ProcessMessageAsync(
            Order order,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            if (++_deliveryCount <= 1)
            {
                Logger.LogTrace("Abandoning message '{OrderId}'...", order.Id);
                await AbandonMessageAsync();
                Logger.LogTrace("Abandoned message '{OrderId}'", order.Id);
            }
            else
            {
                await _eventGridPublisher.PublishOrderAsync(order, correlationInfo, Logger);
            }
        }
    }
}
