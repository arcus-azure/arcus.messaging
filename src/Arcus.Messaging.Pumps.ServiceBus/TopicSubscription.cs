using System;

namespace Arcus.Messaging.Pumps.ServiceBus 
{
    /// <summary>
    /// Represents the option to control the topic subscription existence during the lifecycle of the <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    [Flags]
    public enum TopicSubscription
    {
        /// <summary>
        /// Don't create any topic subscription during the lifecycle of the message pump.
        /// </summary>
        None = 0,

        /// <summary>
        /// Creates a new topic subscription when the message pump starts.
        /// </summary>
        CreateOnStart = 1,

        /// <summary>
        /// Deletes the new topic subscription when the message pump stops.
        /// </summary>
        DeleteOnStop = 2
    }
}
