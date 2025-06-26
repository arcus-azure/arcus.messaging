using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents the consumer-configurable options to change the behavior of the <see cref="AzureServiceBusMessageRouter"/>.
    /// </summary>
    public class AzureServiceBusMessageRouterOptions : MessageRouterOptions
    {
        /// <summary>
        /// Gets or sets the indication whether or not messages should be automatically marked as completed if no exceptions occurred and processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed.</remarks>
        public bool AutoComplete { get; set; } = true;
    }
}
