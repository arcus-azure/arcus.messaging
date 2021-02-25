using System;
using GuardNet;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    ///     Options to configure how the Azure Service Bus message pump works
    /// </summary>
    public class AzureServiceBusMessagePumpConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpConfiguration"/> class.
        /// </summary>
        public AzureServiceBusMessagePumpConfiguration(AzureServiceBusQueueMessagePumpOptions options)
            : this((AzureServiceBusMessagePumpOptions) options)
        {
            Guard.NotNull(options, nameof(options));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpConfiguration"/> class.
        /// </summary>
        public AzureServiceBusMessagePumpConfiguration(AzureServiceBusTopicMessagePumpOptions options) 
            : this((AzureServiceBusMessagePumpOptions) options)
        {
            Guard.NotNull(options, nameof(options));

            TopicSubscription = options.TopicSubscription;
        }

        private AzureServiceBusMessagePumpConfiguration(AzureServiceBusMessagePumpOptions options)
        {
            Guard.NotNull(options, nameof(options));

            MaxConcurrentCalls = options.MaxConcurrentCalls;
            AutoComplete = options.AutoComplete;
            EmitSecurityEvents = options.EmitSecurityEvents;
            JobId = options.JobId;
            KeyRotationTimeout = options.KeyRotationTimeout;
            Correlation = options.Correlation;
        }

        /// <summary>
        ///     Maximum concurrent calls to process messages
        /// </summary>
        internal int? MaxConcurrentCalls { get; }

        /// <summary>
        /// Gets or sets the value indicating whether or not a new Azure Service Bus Topic subscription has to be created when the <see cref="AzureServiceBusMessagePump"/> starts.
        /// The subscription will be deleted afterwards when the message pump stops if the options <see cref="TopicSubscription.DeleteOnStop"/> is selected.
        /// </summary>
        /// <remarks>
        ///     Provides capability to create and delete these subscriptions. This requires 'Manage' permissions on the Azure Service Bus Topic or namespace.
        /// </remarks>
        internal TopicSubscription TopicSubscription { get; }

        /// <summary>
        ///     Indication whether or not messages should be automatically marked as completed if no exceptions occured and
        ///     processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed</remarks>
        internal bool AutoComplete { get; set; }

        /// <summary>
        /// Gets or sets the flag to indicate whether or not to emit security events during the lifetime of the message pump.
        /// </summary>
        internal bool EmitSecurityEvents { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        internal string JobId { get; set; }

        /// <summary>
        /// Gets or sets the timeout when the message pump tries to restart and re-authenticate during key rotation.
        /// </summary>
        internal TimeSpan KeyRotationTimeout { get; set; }
        
        /// <summary>
        /// Gets or sets the options to control the correlation information during the receiving of Azure Service Bus messages.
        /// </summary>
        internal AzureServiceBusCorrelationOptions Correlation { get; }
    }
}
