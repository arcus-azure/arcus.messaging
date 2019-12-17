using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Core.Messages.v1
{
    public class Customer
    {
        [JsonProperty]
        public string FirstName { get; private set; }

        [JsonProperty]
        public string LastName { get; private set; }
    }
}