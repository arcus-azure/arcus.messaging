using System;
using System.Text.Json.Serialization;

namespace Arcus.Messaging.Tests.Core.Messages.v1
{
    public class Order
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("articleNumber")]
        public string ArticleNumber { get; set; }

        [JsonPropertyName("customer")]
        public Customer Customer { get; set; }

        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }
    }
}