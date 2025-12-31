namespace Arcus.Messaging.Tests.Core.Events.v1
{
    public class OrderCreatedEventData
    {
        public string Id { get; set; }
        public int Amount { get; set; }
        public string ArticleNumber { get; set; }
        public string CustomerName { get; set; }
        public OrderCreatedCorrelationInfo CorrelationInfo { get; set; }
        public OrderCreatedEventMessageContext MessageContext { get; set; }
    }

    public class OrderCreatedCorrelationInfo
    {
        public string OperationId { get; set; }
        public string TransactionId { get; set; }
        public string OperationParentId { get; set; }
    }

    public class OrderCreatedEventMessageContext
    {
        public string EntityName { get; set; }
        public string SubscriptionName { get; set; }
    }
}
