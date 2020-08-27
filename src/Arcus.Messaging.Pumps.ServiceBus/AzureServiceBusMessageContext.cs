using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
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
        ///     Token used to lock an individual message for processing
        /// </summary>
        public string LockToken { get; }

        /// <summary>
        ///     Amount of times a message was delivered
        /// </summary>
        /// <remarks>This increases when a message is abandoned and re-delivered for processing</remarks>
        public int DeliveryCount { get; }

        /// <summary>
        ///     Gets the unique ID on which message pump tis message was processed.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="messageId">Unique identifier of the message</param>
        /// <param name="jobId">Unique identifier of the message pump</param>
        /// <param name="systemProperties">Contextual properties provided on the message provided by the Service Bus runtime</param>
        /// <param name="properties">Contextual properties provided on the message provided by the message publisher</param>
        public AzureServiceBusMessageContext(
            string messageId, 
            string jobId,
            Message.SystemPropertiesCollection systemProperties,
            IDictionary<string, object> properties)
            : base(messageId, properties)
        {
            Guard.NotNull(systemProperties, nameof(systemProperties));
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

            JobId = jobId;
            SystemProperties = systemProperties;
            LockToken = systemProperties.LockToken;
            DeliveryCount = systemProperties.DeliveryCount;
        }
    }
}