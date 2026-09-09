using System.Text.Json.Serialization;
using Arcus.Messaging.Tests.Core.Messages.v1;

namespace Arcus.Messaging.Tests.Core.Messages.v2
{
    public class OrderV2
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("articleNumber")]
        public string ArticleNumber { get; set; }

        [JsonPropertyName("customer")]
        public Customer Customer { get; set; }
        
        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}
