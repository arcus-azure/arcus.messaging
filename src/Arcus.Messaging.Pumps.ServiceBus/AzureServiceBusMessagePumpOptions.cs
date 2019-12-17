namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Options to configure how the Azure Service Bus message pump works
    /// </summary>
    public class AzureServiceBusMessagePumpOptions
    {
        /// <summary>
        ///     Maximum concurrent calls to process messages
        /// </summary>
        public int? MaxConcurrentCalls { get; set; }

        /// <summary>
        ///     Indication whether or not messages should be automatically marked as completed if no exceptions occured and
        ///     processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed</remarks>
        public bool AutoComplete { get; set; }

        /// <summary>
        ///     Default settings
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions Default => new AzureServiceBusMessagePumpOptions {AutoComplete = true};
    }
}