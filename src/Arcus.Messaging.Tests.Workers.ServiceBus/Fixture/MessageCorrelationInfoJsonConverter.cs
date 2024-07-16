using System;
using Arcus.Messaging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.Fixture
{
    public class MessageCorrelationInfoJsonConverter : JsonConverter<MessageCorrelationInfo>
    {
        public override void WriteJson(JsonWriter writer, MessageCorrelationInfo? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override MessageCorrelationInfo? ReadJson(
            JsonReader reader,
            Type objectType,
            MessageCorrelationInfo? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            string operationId = json["OperationId"]?.ToString();
            string transactionId = json["TransactionId"]?.ToString();
            string operationParentId = json["OperationParentId"]?.ToString();

            return new MessageCorrelationInfo(operationId, transactionId, operationParentId);
        }
    }
}