using Arcus.EventGrid.Contracts;
using Arcus.Messaging.Abstractions;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Contracts.Events.v1
{
    public class OrderCreatedEvent : EventGridEvent<OrderCreatedEventData>
    {
        private const string DefaultDataVersion = "1";
        private const string DefaultEventType = "Arcus.Samples.Orders.OrderCreated";

        public OrderCreatedEvent(string eventId, string orderId, int amount, string articleNumber, string customerName, MessageCorrelationInfo correlationInfo)
            : base(eventId, $"customer/{customerName}",
                new OrderCreatedEventData(orderId, amount, articleNumber, customerName, correlationInfo), DefaultDataVersion,
                DefaultEventType)
        {
        }

        [JsonConstructor]
        private OrderCreatedEvent()
        {
        }
    }
}