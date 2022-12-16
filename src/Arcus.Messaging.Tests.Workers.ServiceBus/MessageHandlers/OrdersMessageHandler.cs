using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers 
{
    public class OrdersMessageHandler : IMessageHandler<Order>
    {
        private readonly EventGridPublisherClient _eventGridPublisher;

        public OrdersMessageHandler(
            IAzureClientFactory<EventGridPublisherClient> clientFactory, 
            ILogger<OrdersMessageHandler> logger)
        {
            Guard.NotNull(clientFactory, nameof(clientFactory));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = clientFactory.CreateClient("Default");
            Logger = logger;
        }

        protected ILogger<OrdersMessageHandler> Logger { get; }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="order">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            Order order,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await _eventGridPublisher.PublishOrderAsync(order, correlationInfo, Logger);
        }
    }
}