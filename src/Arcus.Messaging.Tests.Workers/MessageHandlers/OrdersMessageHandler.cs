using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersMessageHandler : IAzureServiceBusMessageHandler
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<OrdersMessageHandler> _logger;

        public OrdersMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrdersMessageHandler> logger)
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
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry & processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<MessageProcessResult> ProcessMessageAsync(
            Message message, 
            AzureServiceBusMessageContext messageContext, 
            MessageCorrelationInfo correlationInfo, 
            CancellationToken cancellationToken)
        {
            Encoding encoding = messageContext.GetMessageEncodingProperty();
            if (message.TryReadBodyAsJson(encoding, out Order order))
            {
                _logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", 
                                       order.Id, order.Amount, order.ArticleNumber, order.Customer.FirstName, order.Customer.LastName);

                await PublishEventToEventGridAsync(order, correlationInfo.OperationId, correlationInfo);

                _logger.LogInformation("Order {OrderId} processed", order.Id);
                return MessageProcessResult.Processed;
            }

            _logger.LogError("Cannot deserialize the Service Bus message to a 'Order'");
            return MessageProcessResult.NotSupported;
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
