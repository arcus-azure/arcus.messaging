using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Azure.Core.Amqp;
using Bogus;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Extensions to create more easily received <see cref="ServiceBusReceivedMessage"/> instances from typed message bodies.
    /// </summary>
    public static class ObjectExtensions
    {
        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Creates an <see cref="ServiceBusReceivedMessage"/> based on the given <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The custom typed message body to wrap inside an Azure Service Bus message.</param>
        /// <param name="operationId">The optional correlation operation ID to add to the message.</param>
        /// <param name="applicationProperties">The set of additional application properties to include in the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> is <c>null</c>.</exception>
        public static ServiceBusReceivedMessage AsServiceBusReceivedMessage(
            this object messageBody,
            string operationId = null,
            IDictionary<string, object> applicationProperties = null)
        {
            ArgumentNullException.ThrowIfNull(messageBody);

            string serializedMessageBody = JsonConvert.SerializeObject(messageBody);
            byte[] rawMessage = Encoding.UTF8.GetBytes(serializedMessageBody);

            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromBytes(rawMessage),
                messageId: BogusGenerator.Random.Guid().ToString(),
                correlationId: operationId,
                properties: applicationProperties);
            
            return serviceBusMessage;
        }
    }
}
