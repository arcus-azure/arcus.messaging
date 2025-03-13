using System;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents the option to control the topic subscription existence during the lifecycle of the <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as automatic Azure Service bus topic subscription creation and deletion will not be supported anymore")]
    public enum TopicSubscription
    {
        /// <summary>
        /// Don't create any Azure Service Bus Topic subscription during the lifecycle of the <see cref="AzureServiceBusMessagePump"/>.
        /// </summary>
        None = 0,

        /// <summary>
        /// Creates a new Azure Service Bus Topic subscription when the message pump starts, and deletes it
        /// again when the message pump stops.
        /// </summary>
        Automatic = 1,
    }
}
