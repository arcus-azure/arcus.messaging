using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.Fixture
{
    public class MessageCorrelationInfoJsonConverter : JsonConverter<MessageCorrelationInfo>
    {
        public override MessageCorrelationInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string operationId = null;
            string transactionId = null;
            string operationParentId = null;

            while (reader.Read())
            {
                if(reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if(reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string propertyName = reader.GetString();

                reader.Read();

                switch (propertyName)
                {
                    case "OperationId":
                        operationId = reader.GetString();
                        break;
                    case "TransactionId":
                        transactionId = reader.GetString();
                        break;
                    case "OperationParentId":
                        operationParentId = reader.GetString();
                        break;
                }
            }

            return new MessageCorrelationInfo(operationId, transactionId, operationParentId);
        }

        public override void Write(Utf8JsonWriter writer, MessageCorrelationInfo value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}