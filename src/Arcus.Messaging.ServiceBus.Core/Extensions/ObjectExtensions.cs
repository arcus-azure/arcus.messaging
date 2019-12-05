using System.Text;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Arcus.Messaging.ServiceBus.Core.Extensions
{
    public static class MessageExtensions
    {
        /// <summary>
        ///     Creates an Azure Service Bus Message for a message body
        /// </summary>
        /// <param name="messageBody">Body of the Service Bus message to process</param>
        /// <param name="operationId"> Unique identifier that spans one operation end-to-end</param>
        /// <param name="encoding">Encoding to use during serialization. Defaults to UTF8</param>
        /// <returns>Azure Service Bus Message</returns>
        public static Message AsServiceBusMessage(this object messageBody, string operationId = null, Encoding encoding = null)
        {
            Guard.NotNull(messageBody, nameof(messageBody));

            encoding ??= Encoding.UTF8;

            var serializedMessageBody = JsonConvert.SerializeObject(messageBody);
            var rawMessage = encoding.GetBytes(serializedMessageBody);

            var serviceBusMessage = new Message(rawMessage)
            {
                UserProperties = { { PropertyNames.Encoding, encoding.WebName } },
                CorrelationId = operationId
            };

            return serviceBusMessage;
        }
    }
}