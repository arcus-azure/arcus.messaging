using Arcus.Messaging.Tests.Core.Messages.v1;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Core.Messages.v2
{
    public class OrderV2
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public int Amount { get; set; }

        [JsonProperty]
        public string ArticleNumber { get; set; }

        [JsonProperty]
        public Customer Customer { get; set; }
        
        [JsonProperty]
        public int Status { get; set; }
    }
}
