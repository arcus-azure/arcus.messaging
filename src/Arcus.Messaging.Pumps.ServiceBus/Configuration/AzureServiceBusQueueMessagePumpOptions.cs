using System;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration 
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusQueueMessagePumpOptions : AzureServiceBusMessagePumpOptionsBase
    {
        /// <summary>
        ///     Default settings
        /// </summary>
        internal static AzureServiceBusQueueMessagePumpOptions Default => new AzureServiceBusQueueMessagePumpOptions
        {
            AutoComplete = true,
            JobId = Guid.NewGuid().ToString(),
        };
    }
}