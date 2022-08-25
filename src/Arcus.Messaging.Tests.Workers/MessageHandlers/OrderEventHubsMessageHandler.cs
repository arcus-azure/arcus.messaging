using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrderEventHubsMessageHandler : IAzureEventHubsMessageHandler<Order>
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly IMessageCorrelationInfoAccessor _correlationAccessor;
        private readonly ILogger<OrderEventHubsMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderEventHubsMessageHandler" /> class.
        /// </summary>
        public OrderEventHubsMessageHandler(
            IEventGridPublisher eventGridPublisher, 
            IMessageCorrelationInfoAccessor correlationAccessor,
            ILogger<OrderEventHubsMessageHandler> logger)
        {
            _eventGridPublisher = eventGridPublisher;
            _correlationAccessor = correlationAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        public async Task ProcessMessageAsync(
            Order message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            EnsureSameCorrelation(correlationInfo);
            await _eventGridPublisher.PublishOrderAsync(message, correlationInfo, _logger);
        }

        private void EnsureSameCorrelation(MessageCorrelationInfo correlationInfo)
        {
            MessageCorrelationInfo registeredCorrelation = _correlationAccessor.GetCorrelationInfo();
            Assert.NotNull(registeredCorrelation);
            Assert.Equal(registeredCorrelation.OperationId, correlationInfo.OperationId);
            Assert.Equal(registeredCorrelation.TransactionId, correlationInfo.TransactionId);
            Assert.Equal(registeredCorrelation.OperationParentId, correlationInfo.OperationParentId);
            Assert.Equal(registeredCorrelation.CycleId, correlationInfo.CycleId);
        }
    }
}
