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
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrderEventHubsFallbackMessageHandler : IFallbackMessageHandler
    {
        private readonly IAzureEventHubsMessageHandler<Order> _orderMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderEventHubsFallbackMessageHandler" /> class.
        /// </summary>
        public OrderEventHubsFallbackMessageHandler(
            IEventGridPublisher eventGridPublisher,
            IMessageCorrelationInfoAccessor correlationAccessor,
            ILogger<OrderEventHubsMessageHandler> logger)
        {
            _orderMessageHandler = new OrderEventHubsMessageHandler(eventGridPublisher, correlationAccessor, logger);
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">
        ///     The information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to cancel the processing.</param>
        public async Task ProcessMessageAsync(
            string message,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var order = JsonConvert.DeserializeObject<Order>(message);
            var context = AzureEventHubsMessageContext.CreateFrom(
                new EventData(message),
                "namespace",
                "consumer group",
                "eventhubs name");

            await _orderMessageHandler.ProcessMessageAsync(order, context, correlationInfo, cancellationToken);
        }
    }
}
