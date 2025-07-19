using System;
using Arcus.Messaging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.Fixture
{
    public class MessageCorrelationInfoJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            string operationId = json["OperationId"]?.ToString();
            string transactionId = json["TransactionId"]?.ToString();
            string operationParentId = json["OperationParentId"]?.ToString();

            return new MessageCorrelationInfo(operationId, transactionId, operationParentId);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MessageCorrelationInfo);
        }
    }
}