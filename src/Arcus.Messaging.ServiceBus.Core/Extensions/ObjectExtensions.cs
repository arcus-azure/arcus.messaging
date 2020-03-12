using System.Text;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    public static class ObjectExtensions
    {
        private const string JsonContentType = "application/json";

        /// <summary>
        ///     Creates an Azure Service Bus Message for a message body
        /// </summary>
        /// <param name="messageBody">Body of the Service Bus message to process</param>
        /// <param name="operationId">Unique identifier that spans one operation end-to-end</param>
        /// <param name="transactionId">Unique identifier that spans one or more operations and are considered a transaction/session</param>
        /// <param name="encoding">Encoding to use during serialization. Defaults to UTF8</param>
        /// <returns>Azure Service Bus Message</returns>
        public static Message AsServiceBusMessage(this object messageBody, string operationId = null, string transactionId = null, Encoding encoding = null)
        {
            Guard.NotNull(messageBody, nameof(messageBody));

            encoding = encoding ?? Encoding.UTF8;

            string serializedMessageBody = JsonConvert.SerializeObject(messageBody);
            byte[] rawMessage = encoding.GetBytes(serializedMessageBody);

            var serviceBusMessage = new Message(rawMessage)
            {
                UserProperties =
                {
                    { PropertyNames.ContentType, JsonContentType },
                    { PropertyNames.Encoding, encoding.WebName }
                },
                CorrelationId = operationId
            };

            if (string.IsNullOrWhiteSpace(transactionId) == false)
            {
                serviceBusMessage.UserProperties.Add(PropertyNames.TransactionId, transactionId);
            }

            return serviceBusMessage;
        }
    }
}