using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>, IMessageHandler<Order>
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<OrdersAzureServiceBusMessageHandler> _logger;

        public OrdersAzureServiceBusMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrdersAzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = eventGridPublisher;
            _logger = logger;
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="azureMessageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await ProcessMessageAsync(message, messageContext: azureMessageContext, correlationInfo, cancellationToken);
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="order">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry & processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            Order order, 
            MessageContext messageContext, 
            MessageCorrelationInfo correlationInfo, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", 
                                   order.Id, order.Amount, order.ArticleNumber, order.Customer.FirstName, order.Customer.LastName);

            await PublishEventToEventGridAsync(order, correlationInfo.OperationId, correlationInfo);

            _logger.LogInformation("Order {OrderId} processed", order.Id);
        }

        private async Task PublishEventToEventGridAsync(Order orderMessage, string operationId, MessageCorrelationInfo correlationInfo)
        {
            var orderCreatedEvent = new OrderCreatedEvent(operationId, orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber,
                                                          $"{orderMessage.Customer.FirstName} {orderMessage.Customer.LastName}", correlationInfo);
            await _eventGridPublisher.PublishAsync(orderCreatedEvent);

            _logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
