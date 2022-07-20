using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump.EventHubs.MessageHandling
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
            _logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", 
                message.Id, message.Amount, message.ArticleNumber, message.Customer.FirstName, message.Customer.LastName);

            EnsureSameCorrelation(correlationInfo);
            await PublishEventToEventGridAsync(message, correlationInfo.OperationId, correlationInfo);

            _logger.LogInformation("Order {OrderId} processed", message.Id);
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

        protected async Task PublishEventToEventGridAsync(Order orderMessage, string operationId, MessageCorrelationInfo correlationInfo)
        {
            var eventData = new OrderCreatedEventData(
                orderMessage.Id,
                orderMessage.Amount,
                orderMessage.ArticleNumber,
                $"{orderMessage.Customer.FirstName} {orderMessage.Customer.LastName}",
                correlationInfo);
            var orderCreatedEvent = new CloudEvent(
                CloudEventsSpecVersion.V1_0,
                "OrderCreatedEvent",
                new Uri("http://test-host"),
                operationId,
                DateTime.UtcNow)
            {
                Data = eventData,
                DataContentType = new ContentType("application/json")
            };

            await _eventGridPublisher.PublishAsync(orderCreatedEvent);

            _logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
