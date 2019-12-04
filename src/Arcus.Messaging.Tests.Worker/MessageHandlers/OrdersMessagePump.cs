using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Contracts.Events.v1;
using Arcus.Messaging.Tests.Contracts.v1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Worker.MessageHandlers
{
    public class OrdersMessagePump : AzureServiceBusMessagePump<Order>
    {
        private readonly IEventGridPublisher _eventGridPublisher;

        public OrdersMessagePump(IConfiguration configuration, ILogger<OrdersMessagePump> logger)
            : base(configuration, logger)
        {
            var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
            var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

            _eventGridPublisher = EventGridPublisherBuilder
                .ForTopic(eventGridTopic)
                .UsingAuthenticationKey(eventGridKey)
                .Build();
        }

        protected override async Task ProcessMessageAsync(Order orderMessage,
            AzureServiceBusMessageContext messageContext, MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Logger.LogInformation(
                "Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}",
                orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, orderMessage.Customer.FirstName,
                orderMessage.Customer.LastName);

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            await PublishEventToEventGridAsync(orderMessage);

            Logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
        }

        private async Task PublishEventToEventGridAsync(Order orderMessage)
        {
            var orderCreatedEvent = new OrderCreatedEvent(orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber,
                $"{orderMessage.Customer.FirstName} {orderMessage.Customer.LastName}");
            await _eventGridPublisher.Publish(orderCreatedEvent);

            Logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
        }
    }
}