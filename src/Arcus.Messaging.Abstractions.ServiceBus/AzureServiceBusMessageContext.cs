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
        [Obsolete("Use the " + nameof(ServiceBusMessageContextBuilder) + " to construct message contexts")]
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
        [Obsolete("Use the " + nameof(ServiceBusMessageContextBuilder) + " to construct message contexts")]
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

        internal AzureServiceBusMessageContext(
            string messageId,
            string jobId,
            AzureServiceBusSystemProperties systemProperties,
            IReadOnlyDictionary<string, object> applicationProperties,
            ServiceBusEntityType entityType,
            string entityName,
            string fullyQualifiedNamespace)
            : base(messageId, jobId, applicationProperties?.ToDictionary(item => item.Key, item => item.Value) ?? throw new ArgumentNullException(nameof(applicationProperties)))
        {
            if (systemProperties is null)
            {
                throw new ArgumentNullException(nameof(systemProperties));
            }

            SystemProperties = systemProperties;
            LockToken = systemProperties.LockToken;
            DeliveryCount = systemProperties.DeliveryCount;
            EntityType = entityType;

            FullyQualifiedNamespace = fullyQualifiedNamespace;
            EntityPath = entityName;
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
    }

    /// <summary>
    /// Represents the build process of constructing <see cref="AzureServiceBusMessageContext"/> instances.
    /// </summary>
    public class ServiceBusMessageContextBuilder
    {
        private readonly string _messageId;
        private readonly AzureServiceBusSystemProperties _systemProperties;
        private readonly IReadOnlyDictionary<string, object> _applicationProperties;

        private string _fullyQualifiedNamespace = "<not-available>", _entityName = "<not-available>", _jobId = "<not-available>";
        private ServiceBusEntityType _entityType = ServiceBusEntityType.Unknown;

        private ServiceBusMessageContextBuilder(ServiceBusReceivedMessage message)
        {
            _messageId = message.MessageId;
            _systemProperties = message.GetSystemProperties();
            _applicationProperties = message.ApplicationProperties;
        }

        /// <summary>
        /// Start creating a new <see cref="AzureServiceBusMessageContext"/> base on the received <paramref name="message"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public static ServiceBusMessageContextBuilder CreateFor(ServiceBusReceivedMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new ServiceBusMessageContextBuilder(message);
        }

        /// <summary>
        /// Provide the Azure Service bus <paramref name="receiver"/> to the construction, where the message was received.
        /// </summary>
        /// <param name="receiver">The instance that received the message for this context.</param>
        /// <param name="entityType">The type of entity that the <paramref name="receiver"/> is active on.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="receiver"/> is <c>null</c>.</exception>
        public ServiceBusMessageContextBuilder WithReceiver(ServiceBusReceiver receiver, ServiceBusEntityType entityType)
        {
            if (receiver is null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            _fullyQualifiedNamespace = receiver.FullyQualifiedNamespace;
            _entityType = entityType;
            _entityName = receiver.EntityPath;

            return this;
        }

        /// <summary>
        /// Provide the message pump unique <paramref name="jobId"/> that was responsible for starting up the Azure Service bus receival.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public ServiceBusMessageContextBuilder WithMessagePump(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank message pump unique job ID to construct a message context", nameof(jobId));
            }

            _jobId = jobId;
            return this;
        }

        /// <summary>
        /// Creates a <see cref="AzureServiceBusMessageContext"/> based on the previously configured information.
        /// </summary>
        public AzureServiceBusMessageContext Build()
        {
            return new AzureServiceBusMessageContext(
                _messageId,
                _jobId,
                _systemProperties,
                _applicationProperties,
                _entityType,
                _entityName,
                _fullyQualifiedNamespace);
        }
    }
}