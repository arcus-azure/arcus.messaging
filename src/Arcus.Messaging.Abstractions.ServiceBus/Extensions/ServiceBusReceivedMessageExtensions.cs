using System;
using Arcus.Messaging.Abstractions.ServiceBus;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusReceivedMessage"/> related to retrieving sub-information from the message.
    /// </summary>
    public static class ServiceBusReceivedMessageExtensions
    {
        /// <summary>
        /// Gets the <see cref="AzureServiceBusSystemProperties"/>, which is used to store properties that are set by the system.
        /// </summary>
        /// <param name="message">The received Azure Service Bus message to extract the system properties from.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public static AzureServiceBusSystemProperties GetSystemProperties(this ServiceBusReceivedMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return AzureServiceBusSystemProperties.CreateFrom(message);
        }

        /// <summary>
        /// Gets the <see cref="AzureServiceBusMessageContext"/>, which contains the user and system messaging information about the received Azure Service Bus <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The received Azure Service Bus message to extract the messaging context from.</param>
        /// <param name="jobId">The unique ID to identify the current messaging job, pump or router that is handling the received <paramref name="message"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Use the " + nameof(ServiceBusMessageContextBuilder) + " to construct message contexts")]
        public static AzureServiceBusMessageContext GetMessageContext(this ServiceBusReceivedMessage message, string jobId)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return message.GetMessageContext(jobId, ServiceBusEntityType.Unknown);
        }

        /// <summary>
        /// Gets the <see cref="AzureServiceBusMessageContext"/>, which contains the user and system messaging information about the received Azure Service Bus <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The received Azure Service Bus message to extract the messaging context from.</param>
        /// <param name="jobId">The unique ID to identify the current messaging job, pump or router that is handling the received <paramref name="message"/>.</param>
        /// <param name="entityType">The type of the Azure Service Bus entity on which a message was received.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Use the " + nameof(ServiceBusMessageContextBuilder) + " to construct message contexts")]
        public static AzureServiceBusMessageContext GetMessageContext(this ServiceBusReceivedMessage message, string jobId, ServiceBusEntityType entityType)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new AzureServiceBusMessageContext(message.MessageId, jobId, message.GetSystemProperties(), message.ApplicationProperties, entityType);
        }
    }
}
