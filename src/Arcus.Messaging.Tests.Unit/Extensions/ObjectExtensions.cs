using System;
using System.Reflection;
using System.Text;
using Azure.Core.Amqp;
using GuardNet;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Extensions to create more easily received <see cref="ServiceBusReceivedMessage"/> instances from typed message bodies.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Creates an <see cref="ServiceBusReceivedMessage"/> based on the given <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The custom typed message body to wrap inside an Azure Service Bus message.</param>
        /// <param name="applicationPropertyKey">The additional application property key to add to the message.</param>
        /// <param name="applicationPropertyValue">The additional application property value to add to the message.</param>
        /// <param name="operationId">The optional correlation operation ID to add to the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> is <c>null</c>.</exception>
        public static ServiceBusReceivedMessage AsServiceBusReceivedMessage(
            this object messageBody, 
            string applicationPropertyKey = null, 
            object applicationPropertyValue = null, 
            string operationId = null)
        {
            Guard.NotNull(messageBody, nameof(messageBody), "Requires a message body to wrap in an received Azure Service Bus message");
            
            string serializedMessageBody = JsonConvert.SerializeObject(messageBody);
            byte[] rawMessage = Encoding.UTF8.GetBytes(serializedMessageBody);
            var amqp = new AmqpAnnotatedMessage(new AmqpMessageBody(new[] {new ReadOnlyMemory<byte>(rawMessage)}));
            
            if (operationId is null)
            {
                amqp.Properties.CorrelationId = new AmqpMessageId();
            }
            else
            {
                amqp.Properties.CorrelationId = new AmqpMessageId(operationId);
            }

            if (applicationPropertyKey != null)
            {
                amqp.ApplicationProperties[applicationPropertyKey] = applicationPropertyValue;
            }

            var serviceBusMessage = (ServiceBusReceivedMessage) Activator.CreateInstance(
                type: typeof(ServiceBusReceivedMessage),
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                args: new object[] {amqp},
                culture: null,
                activationAttributes: null);
            
            return serviceBusMessage;
        }
    }
}
