using System;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersAzureServiceBusAbandonFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
    {
        private readonly IEventGridPublisher _eventGridPublisher;

        public OrdersAzureServiceBusAbandonFallbackMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrdersAzureServiceBusMessageHandler> logger)
            : base(logger)
        {
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));

            _eventGridPublisher = eventGridPublisher;
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
        public override async Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            if (azureMessageContext.SystemProperties.DeliveryCount <= 1)
            {
                Logger.LogTrace("Abandoning message '{MessageId}'...", message.MessageId);
                await AbandonAsync(message);
                Logger.LogTrace("Abandoned message '{MessageId}'", message.MessageId);
            }
            else
            {
                string json = Encoding.UTF8.GetString(message.Body);
                var order = JsonConvert.DeserializeObject<Order>(json);

                Logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", 
                                      order.Id, order.Amount, order.ArticleNumber, order.Customer.FirstName, order.Customer.LastName);

                await PublishEventToEventGridAsync(order, correlationInfo.OperationId, correlationInfo);

                Logger.LogInformation("Order {OrderId} processed", order.Id);
            }
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

            Logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
