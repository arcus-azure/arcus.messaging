using System;
using GuardNet;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    ///     Options to configure how the Azure Service Bus message pump works
    /// </summary>
    public class AzureServiceBusMessagePumpConfiguration
    {
        private int _maximumUnauthorizedExceptionsBeforeRestart;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpConfiguration"/> class.
        /// </summary>
        /// <param name="options">The specific options for Azure Service Bus.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        internal AzureServiceBusMessagePumpConfiguration(AzureServiceBusMessagePumpOptions options)
        {
            Guard.NotNull(options, nameof(options), "Requires an Azure Service Bus options");

            MaxConcurrentCalls = options.MaxConcurrentCalls;
            AutoComplete = options.AutoComplete;
            JobId = options.JobId;
            KeyRotationTimeout = options.KeyRotationTimeout;
            
            _maximumUnauthorizedExceptionsBeforeRestart = options.MaximumUnauthorizedExceptionsBeforeRestart;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpConfiguration"/> class.
        /// </summary>
        /// <param name="options">The specific options for Azure Service Bus Topics.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public AzureServiceBusMessagePumpConfiguration(AzureServiceBusQueueMessagePumpOptions options) : this((AzureServiceBusMessagePumpOptions) options)
        {
            Guard.NotNull(options, nameof(options), "Requires an Azure Service Bus options for Topics");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpConfiguration"/> class.
        /// </summary>
        /// <param name="options">The specific options for Azure Service Bus Queues.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public AzureServiceBusMessagePumpConfiguration(AzureServiceBusTopicMessagePumpOptions options) : this((AzureServiceBusMessagePumpOptions) options)
        {
            Guard.NotNull(options, nameof(options), "Requires an Azure Service Bus options for Queues");
            TopicSubscription = options.TopicSubscription;
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
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        internal string JobId { get; set; }

        /// <summary>
        /// Gets or sets the timeout when the message pump tries to restart and re-authenticate during key rotation.
        /// </summary>
        internal TimeSpan KeyRotationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the fallback when the Azure Key Vault notification doesn't get delivered correctly,
        /// how many times should the message pump run into an <see cref="UnauthorizedException"/> before restarting.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        public int MaximumUnauthorizedExceptionsBeforeRestart
        {
            get => _maximumUnauthorizedExceptionsBeforeRestart;
            set
            {
                Guard.NotLessThan(value, 0, nameof(value), "Requires an unauthorized exceptions count that's greater than zero");
                _maximumUnauthorizedExceptionsBeforeRestart = value;
            }
        }
    }
}
