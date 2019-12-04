using System;
using Arcus.EventGrid.Contracts;

namespace Arcus.Messaging.Tests.Contracts.Events.v1
{
    public class OrderCreatedEvent : Event<OrderCreatedEventData>
    {
        public override string DataVersion { get; } = "1";
        public override string EventType { get; } = "Arcus.Samples.Orders.OrderCreated";

        public OrderCreatedEvent(string id, int amount, string articleNumber, string customerName)
        : base(Guid.NewGuid().ToString(), $"customer/{customerName}")
        {
            Data.Id = id;
            Data.Amount = amount;
            Data.ArticleNumber = articleNumber;
            Data.CustomerName = customerName;
        }

        public OrderCreatedEvent()
        {
        }
    }
}