using System;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusTopicMessagePumpOptions : AzureServiceBusMessagePumpOptionsBase
    {
        /// <summary>
        /// Gets or sets the value indicating whether or not a new Azure Service Bus Topic subscription has to be created when the <see cref="AzureServiceBusMessagePump{TMessage}"/> starts.
        /// The subscription will be deleted afterwards when the message pump stops if the options <see cref="TopicSubscription.DeleteOnStop"/> is selected.
        /// </summary>
        /// <remarks>
        ///     Provides capability to create and delete these subscriptions. This requires 'Manage' permissions on the Azure Service Bus Topic or namespace.
        /// </remarks>
        public TopicSubscription TopicSubscription { get; set; }

        /// <summary>
        ///     Default settings
        /// </summary>
        internal static AzureServiceBusTopicMessagePumpOptions Default => new AzureServiceBusTopicMessagePumpOptions
        {
            AutoComplete = true,
            JobId = Guid.NewGuid().ToString(),
            TopicSubscription = TopicSubscription.CreateOnStart | TopicSubscription.DeleteOnStop
        };
    }
}
