using System;
using System.Text.Json.Serialization;

namespace Arcus.Messaging.Tests.Core.Messages.v1
{
    public class Shipment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
