using System;
using GuardNet;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Options to configure how the Azure Service Bus message pump works
    /// </summary>
    public class AzureServiceBusMessagePumpOptions
    {
        private int? _maxConcurrentCalls;

        /// <summary>
        ///     Maximum concurrent calls to process messages
        /// </summary>
        public int? MaxConcurrentCalls
        {
            get => _maxConcurrentCalls;
            set
            {
                Guard.For<ArgumentException>(() => value <= 0);
                _maxConcurrentCalls = value;
            }
        }

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