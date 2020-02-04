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
                if (value != null)
                {
                    Guard.For<ArgumentException>(() => value <= 0, "Max concurrent calls has to be 1 or above.");
                }

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
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether or not a new Azure Service Bus Topic subscription has to be created when the <see cref="AzureServiceBusMessagePump{TMessage}"/> starts.
        /// The subscription will be deleted afterwards when the message pump stops.
        /// REMARK: requires at least 'Manage' permissions on the Azure Service Bus Topic to create and delete these subscriptions.
        /// </summary>
        /// <remarks>
        ///     Requires at least 'Manage' permissions on the Azure Service Bus Topic to create and delete these subscriptions.
        /// </remarks>
        public bool IncludeTopicSubscription { get; set; }

        /// <summary>
        ///     Default settings
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions Default => new AzureServiceBusMessagePumpOptions
        {
            AutoComplete = true,
            JobId = Guid.NewGuid().ToString()
        };
    }
}