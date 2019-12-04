namespace Arcus.Messaging.Tests.Contracts.Events.v1
{
    public class OrderCreatedEventData
    {
        public string Id { get; set; }
        public int Amount { get; set; }
        public string ArticleNumber { get; set; }
        public string CustomerName { get; set; }
    }
}
