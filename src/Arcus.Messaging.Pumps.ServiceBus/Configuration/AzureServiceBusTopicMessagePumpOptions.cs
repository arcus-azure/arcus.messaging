using System;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    ///     Options to configure a Azure Service Bus topic <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    public class AzureServiceBusTopicMessagePumpOptions : AzureServiceBusMessagePumpOptions
    {
        /// <summary>
        /// Gets or sets the value indicating whether or not a new Azure Service Bus Topic subscription has to be created when the <see cref="AzureServiceBusMessagePump"/> starts.
        /// The subscription will be deleted afterwards when the message pump stops if the options <see cref="TopicSubscription.DeleteOnStop"/> is selected.
        /// </summary>
        /// <remarks>
        ///     Provides capability to create and delete these subscriptions. This requires 'Manage' permissions on the Azure Service Bus Topic or namespace.
        /// </remarks>
        public TopicSubscription TopicSubscription { get; set; } = TopicSubscription.CreateOnStart | TopicSubscription.DeleteOnStop;

        /// <summary>
        ///     Gets the default settings.
        /// </summary>
        public static AzureServiceBusTopicMessagePumpOptions Default => new AzureServiceBusTopicMessagePumpOptions();
    }
}
