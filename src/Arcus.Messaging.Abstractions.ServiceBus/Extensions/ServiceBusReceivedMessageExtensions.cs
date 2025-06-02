using System;
using Arcus.Messaging.Abstractions.ServiceBus;

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
        [Obsolete("Will be removed in v3.0, please use the factory method instead: " + nameof(AzureServiceBusMessageContext) + "." + nameof(AzureServiceBusMessageContext.Create) + " to get the Arcus-created system properties")]
        public static AzureServiceBusSystemProperties GetSystemProperties(this ServiceBusReceivedMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return AzureServiceBusSystemProperties.CreateFrom(message);
        }
    }
}
