using System;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Core.Messages.v1
{
    public class Shipment
    {
        [JsonProperty]
        public int Code { get; set; }
        
        [JsonProperty]
        public DateTimeOffset Date { get; set; }
        
        [JsonProperty]
        public string Description { get; set; }
    }
}
