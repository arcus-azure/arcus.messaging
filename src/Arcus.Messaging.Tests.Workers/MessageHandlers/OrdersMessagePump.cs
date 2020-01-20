using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersMessagePump : AzureServiceBusMessagePump<Order>
    {
        private readonly IEventGridPublisher _eventGridPublisher;

        public OrdersMessagePump(IEventGridPublisher eventGridPublisher, IConfiguration configuration, IServiceProvider serviceProvider, ILogger<OrdersMessagePump> logger)
            : base(configuration, serviceProvider, logger)
        {
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));

            _eventGridPublisher = eventGridPublisher;
        }

        protected override async Task ProcessMessageAsync(Order orderMessage, AzureServiceBusMessageContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, orderMessage.Customer.FirstName, orderMessage.Customer.LastName);

            await PublishEventToEventGridAsync(orderMessage, correlationInfo.OperationId, correlationInfo);

            Logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
        }

        private async Task PublishEventToEventGridAsync(Order orderMessage, string operationId, MessageCorrelationInfo correlationInfo)
        {
            var orderCreatedEvent = new OrderCreatedEvent(operationId, orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber,
                $"{orderMessage.Customer.FirstName} {orderMessage.Customer.LastName}", correlationInfo);
            await _eventGridPublisher.PublishAsync(orderCreatedEvent);

            Logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}