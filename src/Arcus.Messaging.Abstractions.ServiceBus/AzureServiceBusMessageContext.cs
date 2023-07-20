using System;
using System.Collections.Generic;
using System.Linq;
using GuardNet;

namespace Arcus.Messaging.Abstractions.ServiceBus
{
    /// <summary>
    ///     Contextual information concerning an Azure Service Bus message
    /// </summary>
    public class AzureServiceBusMessageContext : MessageContext
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureServiceBusMessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">Unique identifier of the message pump.</param>
        /// <param name="systemProperties">The contextual properties provided on the message provided by the Azure Service Bus runtime.</param>
        /// <param name="properties">The contextual properties provided on the message provided by the message publisher.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="systemProperties"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public AzureServiceBusMessageContext(
            string messageId, 
            string jobId,
            AzureServiceBusSystemProperties systemProperties,
            IReadOnlyDictionary<string, object> properties)
            : base(messageId, jobId, properties.ToDictionary(item => item.Key, item => item.Value))
        {
            Guard.NotNullOrWhitespace(messageId, nameof(messageId), "Requires an ID to identify the message");
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires an job ID that is not blank to identify the message pump");
            Guard.NotNull(systemProperties, nameof(systemProperties), "Requires a set of system properties provided by the Azure Service Bus runtime");
            Guard.NotNull(properties, nameof(properties), "Requires contextual properties provided by the message publisher");

            SystemProperties = systemProperties;
            LockToken = systemProperties.LockToken;
            DeliveryCount = systemProperties.DeliveryCount;
        }

        /// <summary>
        ///     Gets the contextual properties provided on the message provided by the Azure Service Bus runtime
        /// </summary>
        public AzureServiceBusSystemProperties SystemProperties { get; }

        /// <summary>
        ///     Gets the token used to lock an individual message for processing
        /// </summary>
        public string LockToken { get; }

        /// <summary>
        ///     Gets the amount of times a message was delivered
        /// </summary>
        /// <remarks>This increases when a message is abandoned and re-delivered for processing</remarks>
        public int DeliveryCount { get; }
    }
}