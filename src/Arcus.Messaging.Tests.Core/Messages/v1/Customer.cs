using System.Text.Json.Serialization;

namespace Arcus.Messaging.Tests.Core.Messages.v1
{
    public class Customer
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; private set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; private set; }
    }
}