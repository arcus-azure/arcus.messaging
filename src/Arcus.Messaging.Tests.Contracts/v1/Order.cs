using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Contracts.v1
{
    public class Order
    {
        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public int Amount { get; private set; }

        [JsonProperty]
        public string ArticleNumber { get; private set; }

        [JsonProperty]
        public Customer Customer { get; private set; }
    }
}