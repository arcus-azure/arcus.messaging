using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Arcus.Observability.Correlation;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

// ReSharper disable once CheckNamespace
namespace Arcus.EventGrid.Publishing.Interfaces
{
    // ReSharper disable once InconsistentNaming
    public static class IEventGridPublisherExtensions
    {
        public static async Task PublishOrderAsync(
            this IEventGridPublisher publisher, 
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
                CloudEventsSpecVersion.V1_0,
                "OrderCreatedEvent",
                new Uri("http://test-host"),
                correlationInfo.OperationId,
                DateTime.UtcNow)
            {
                Data = eventData,
                DataContentType = new ContentType("application/json")
            };

            await publisher.PublishAsync(orderCreatedEvent);
            logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject); 
            logger.LogInformation("Order {OrderId} processed", message.Id);
        }

        public static async Task PublishOrderAsync(
            this IEventGridPublisher publisher,
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
                CloudEventsSpecVersion.V1_0,
                "OrderCreatedEvent",
                new Uri("http://test-host"),
                correlationInfo.OperationId,
                DateTime.UtcNow)
            {
                Data = eventData,
                DataContentType = new ContentType("application/json")
            };

            await publisher.PublishAsync(orderCreatedEvent);

            logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}
