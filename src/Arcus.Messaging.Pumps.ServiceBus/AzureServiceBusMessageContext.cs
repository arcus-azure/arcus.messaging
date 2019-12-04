using System.Collections.Generic;
using Arcus.Messaging.Pumps.Abstractions;
using GuardNet;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Contextual information concerning an Azure Service Bus message
    /// </summary>
    public class AzureServiceBusMessageContext : MessageContext
    {
        /// <summary>
        ///     Contextual properties provided on the message provided by the Service Bus runtime
        /// </summary>
        public Message.SystemPropertiesCollection SystemProperties { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="messageId">Unique identifier of the message</param>
        /// <param name="systemProperties">Contextual properties provided on the message provided by the Service Bus runtime</param>
        /// <param name="properties">Contextual properties provided on the message provided by the message publisher</param>
        public AzureServiceBusMessageContext(string messageId, Message.SystemPropertiesCollection systemProperties,
            IDictionary<string, object> properties)
            : base(messageId, properties)
        {
            Guard.NotNull(systemProperties, nameof(systemProperties));

            SystemProperties = systemProperties;
        }
    }
}