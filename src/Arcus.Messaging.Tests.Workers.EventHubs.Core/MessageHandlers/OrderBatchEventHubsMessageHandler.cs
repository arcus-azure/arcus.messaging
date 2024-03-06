using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrderBatchEventHubsMessageHandler : IAzureEventHubsMessageHandler<OrderBatch>
    {
        private readonly IAzureEventHubsMessageHandler<Order> _orderMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBatchEventHubsMessageHandler" /> class.
        /// </summary>
        public OrderBatchEventHubsMessageHandler(
            IAzureClientFactory<EventGridPublisherClient> clientFactory,
            IMessageCorrelationInfoAccessor correlationInfoAccessor,
            ILogger<OrderEventHubsMessageHandler> logger)
        {
            _orderMessageHandler = new OrderEventHubsMessageHandler(clientFactory, correlationInfoAccessor, logger);
        }

        public async Task ProcessMessageAsync(
            OrderBatch message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            foreach (Order order in message.Orders)
            {
                await _orderMessageHandler.ProcessMessageAsync(order, messageContext, correlationInfo, cancellationToken);
            }
        }
    }
}
