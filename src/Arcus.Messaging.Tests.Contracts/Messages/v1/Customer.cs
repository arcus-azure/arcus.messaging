using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Contracts.Messages.v1
{
    public class Customer
    {
        [JsonProperty]
        public string FirstName { get; private set; }

        [JsonProperty]
        public string LastName { get; private set; }
    }
}