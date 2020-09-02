using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersServiceBusFallbackMessageHandler : IAzureServiceBusFallbackMessageHandler
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<OrdersAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersAzureServiceBusMessageHandler"/> class.
        /// </summary>
        /// <param name="eventGridPublisher">The publisher instance to send event messages to Azure Event Grid.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the interaction with Azure Event Grid.</param>
        public OrdersServiceBusFallbackMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrdersAzureServiceBusMessageHandler> logger)
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
            Message message,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            string messageBody = Encoding.UTF8.GetString(message.Body);
            var order = JsonConvert.DeserializeObject<Order>(messageBody);

            _logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}",
                                   order.Id, order.Amount, order.ArticleNumber, order.Customer.FirstName, order.Customer.LastName);

            await PublishEventToEventGridAsync(order, correlationInfo.OperationId, correlationInfo);

            _logger.LogInformation("Order {OrderId} processed", order.Id);
        }

        private async Task PublishEventToEventGridAsync(Order orderMessage, string operationId, MessageCorrelationInfo correlationInfo)
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
