using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.ServiceBus
{
    /// <summary>
    /// Represents the contextual information concerning an Azure Service Bus message.
    /// </summary>
    public class AzureServiceBusMessageContext : MessageContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">Unique identifier of the message pump.</param>
        /// <param name="systemProperties">The contextual properties provided on the message provided by the Azure Service Bus runtime.</param>
        /// <param name="properties">The contextual properties provided on the message provided by the message publisher.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="systemProperties"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, please use the factory method instead: " + nameof(AzureServiceBusMessageContext) + "." + nameof(Create))]
        public AzureServiceBusMessageContext(
            string messageId,
            string jobId,
            AzureServiceBusSystemProperties systemProperties,
            IReadOnlyDictionary<string, object> properties)
            : this(messageId, jobId, systemProperties, properties, ServiceBusEntityType.Unknown)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">Unique identifier of the message pump.</param>
        /// <param name="systemProperties">The contextual properties provided on the message provided by the Azure Service Bus runtime.</param>
        /// <param name="properties">The contextual properties provided on the message provided by the message publisher.</param>
        /// <param name="entityType">The type of the Azure Service Bus entity on which a message with the ID <paramref name="messageId"/> was received.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="systemProperties"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, please use the factory method instead: " + nameof(AzureServiceBusMessageContext) + "." + nameof(Create))]
        public AzureServiceBusMessageContext(
            string messageId,
            string jobId,
            AzureServiceBusSystemProperties systemProperties,
            IReadOnlyDictionary<string, object> properties,
            ServiceBusEntityType entityType)
            : base(messageId, jobId, properties?.ToDictionary(item => item.Key, item => item.Value) ?? throw new ArgumentNullException(nameof(properties)))
        {
            if (systemProperties is null)
            {
                throw new ArgumentNullException(nameof(systemProperties));
            }

            SystemProperties = systemProperties;
            LockToken = systemProperties.LockToken;
            DeliveryCount = systemProperties.DeliveryCount;
            EntityType = entityType;
        }

        private AzureServiceBusMessageContext(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
            : base(message.MessageId, jobId, message.ApplicationProperties.ToDictionary(item => item.Key, item => item.Value))
        {
            FullyQualifiedNamespace = receiver.FullyQualifiedNamespace;
            EntityPath = receiver.EntityPath;
            EntityType = entityType;
            SystemProperties = AzureServiceBusSystemProperties.CreateFrom(message);
            LockToken = message.LockToken;
            DeliveryCount = message.DeliveryCount;
        }

        /// <summary>
        /// Gets the fully qualified Azure Service bus namespace that the message pump is associated with.
        /// This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </summary>
        public string FullyQualifiedNamespace { get; }

        /// <summary>
        /// Gets the path of the Azure Service bus entity that the message pump is connected to,
        /// specific to the Azure Service bus namespace that contains it.
        /// </summary>
        public string EntityPath { get; }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity on which the message was received.
        /// </summary>
        public ServiceBusEntityType EntityType { get; }

        /// <summary>
        /// Gets the contextual properties provided on the message provided by the Azure Service Bus runtime
        /// </summary>
        public AzureServiceBusSystemProperties SystemProperties { get; }

        /// <summary>
        /// Gets the token used to lock an individual message for processing
        /// </summary>
        public string LockToken { get; }

        /// <summary>
        /// Gets the amount of times a message was delivered
        /// </summary>
        /// <remarks>This increases when a message is abandoned and re-delivered for processing</remarks>
        public int DeliveryCount { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="AzureServiceBusMessageContext"/> based on the current Azure Service bus situation.
        /// </summary>
        /// <param name="jobId">The unique ID to identity the Azure Service bus message pump that is responsible for pumping messages from the <paramref name="receiver"/>.</param>
        /// <param name="entityType">The type of Azure Service bus entity that the <paramref name="receiver"/> receives from.</param>
        /// <param name="receiver">The Azure Service bus receiver that is responsible for receiving the <paramref name="message"/>.</param>
        /// <param name="message">The Azure Service bus message that is currently being processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the parameters is <c>null</c>.</exception>
        public static AzureServiceBusMessageContext Create(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
        {
            ArgumentNullException.ThrowIfNull(jobId);
            ArgumentNullException.ThrowIfNull(receiver);
            ArgumentNullException.ThrowIfNull(message);

            return new AzureServiceBusMessageContext(jobId, entityType, receiver, message);
        }
    }
}