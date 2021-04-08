using System;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration 
{
    /// <summary>
    ///     Options to configure a Azure Service Bus queue <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    public class AzureServiceBusQueueMessagePumpOptions : AzureServiceBusMessagePumpOptions
    {
        /// <summary>
        ///     Gets the default settings.
        /// </summary>
        public static AzureServiceBusQueueMessagePumpOptions Default => new AzureServiceBusQueueMessagePumpOptions();
    }
}