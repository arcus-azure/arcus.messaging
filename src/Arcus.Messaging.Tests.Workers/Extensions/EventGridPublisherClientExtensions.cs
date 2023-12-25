using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Arcus.EventGrid.Publishing.Interfaces
{
    // ReSharper disable once InconsistentNaming
    public static class EventGridPublisherClientExtensions
    {
        public static async Task PublishOrderAsync(
            this EventGridPublisherClient publisher, 
            Order message, 
            MessageCorrelationInfo correlationInfo,
            ILogger logger)
        {
            logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", message.Id, message.Amount, message.ArticleNumber, message.Customer.FirstName, message.Customer.LastName);
            var eventData = new OrderCreatedEventData(
                message.Id,
                message.Amount,
                message.ArticleNumber,
                $"{message.Customer.FirstName} {message.Customer.LastName}",
                correlationInfo);

            var orderCreatedEvent = new CloudEvent(
                "http://test-host",
                "OrderCreatedEvent",
                jsonSerializableData: eventData)
            {
                Id = correlationInfo.OperationId,
                Time = DateTimeOffset.UtcNow
            };

            await publisher.SendEventAsync(orderCreatedEvent);
            logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject); 
            logger.LogInformation("Order {OrderId} processed", message.Id);
        }

        public static async Task PublishOrderAsync(
            this EventGridPublisherClient publisher,
            OrderV2 message, 
            MessageCorrelationInfo correlationInfo, 
            ILogger logger)
        {
            logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", message.Id, message.Amount, message.ArticleNumber, message.Customer.FirstName, message.Customer.LastName);
            var eventData = new OrderCreatedEventData(
                message.Id,
                message.Amount,
                message.ArticleNumber,
                $"{message.Customer.FirstName} {message.Customer.LastName}",
                correlationInfo);

            var orderCreatedEvent = new CloudEvent(
                "http://test-host",
                "OrderCreatedEvent",
                jsonSerializableData: eventData)
            {
                Id = correlationInfo.OperationId,
                Time = DateTimeOffset.UtcNow
            };

            await publisher.SendEventAsync(orderCreatedEvent);

            logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
