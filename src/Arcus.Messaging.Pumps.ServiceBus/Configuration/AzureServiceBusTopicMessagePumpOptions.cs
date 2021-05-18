using System;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    ///     Options to configure a Azure Service Bus topic <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    /// TODO: remove 'old' workings after the background jobs package is updated with the new messaging package.
    [Obsolete("Will be removed in the future, please use the " + nameof(AzureServiceBusMessagePumpOptions) + " instead")]
    public class AzureServiceBusTopicMessagePumpOptions : AzureServiceBusMessagePumpOptions
    {
        /// <summary>
        ///     Gets the default settings.
        /// </summary>
        public static AzureServiceBusTopicMessagePumpOptions Default => new AzureServiceBusTopicMessagePumpOptions();
    }
}
