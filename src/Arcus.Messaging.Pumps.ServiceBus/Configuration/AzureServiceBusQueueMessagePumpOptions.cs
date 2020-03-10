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
        internal static AzureServiceBusQueueMessagePumpOptions Default => new AzureServiceBusQueueMessagePumpOptions
        {
            AutoComplete = true,
            JobId = Guid.NewGuid().ToString(),
        };
    }
}