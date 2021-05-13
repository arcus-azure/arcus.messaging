using System;
using System.Text;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    /// <summary>
    /// Extensions on message bodies to more easily create <see cref="ServiceBusMessage"/>s from them.
    /// </summary>
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
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> is <c>null</c>.</exception>
        /// <returns>an <see cref="ServiceBusMessage"/>.</returns>
        public static ServiceBusMessage AsServiceBusMessage(this object messageBody, string operationId = null, string transactionId = null, Encoding encoding = null)
        { 
            Guard.NotNull(messageBody, nameof(messageBody), "Requires a message body to create an Azure Service Bus message this message body");

            encoding = encoding ?? Encoding.UTF8;

            string serializedMessageBody = JsonConvert.SerializeObject(messageBody);
            byte[] rawMessage = encoding.GetBytes(serializedMessageBody);
           
            var serviceBusMessage = new ServiceBusMessage(rawMessage)
            {
                ApplicationProperties =
                {
                    { PropertyNames.ContentType, JsonContentType },
                    { PropertyNames.Encoding, encoding.WebName }
                }
            };
            
            if (operationId != null)
            {
                serviceBusMessage.CorrelationId = operationId;
            }

            if (string.IsNullOrWhiteSpace(transactionId) == false)
            {
                serviceBusMessage.ApplicationProperties.Add(PropertyNames.TransactionId, transactionId);
            }

            return serviceBusMessage;
        }
    }
}