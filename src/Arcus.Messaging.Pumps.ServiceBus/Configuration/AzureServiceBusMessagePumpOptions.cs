using System;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration 
{
    /// <summary>
    /// The general options that configures a <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    public class AzureServiceBusMessagePumpOptions : IAzureServiceBusQueueMessagePumpOptions, IAzureServiceBusTopicMessagePumpOptions
    {
        private int? _maxConcurrentCalls;
        private string _jobId = Guid.NewGuid().ToString();
        private TimeSpan _keyRotationTimeout = TimeSpan.FromSeconds(5);
        private int _maximumUnauthorizedExceptionsBeforeRestart = 5;

        /// <summary>
        /// <para>Gets or sets the value indicating whether or not a new Azure Service Bus Topic subscription has to be created when the <see cref="AzureServiceBusMessagePump"/> starts.</para>
        /// <para>The subscription will be deleted afterwards when the message pump stops if the options <see cref="ServiceBus.TopicSubscription.DeleteOnStop"/> is selected.</para>
        /// </summary>
        /// <remarks>
        ///     Provides capability to create and delete these subscriptions. This requires 'Manage' permissions on the Azure Service Bus Topic or namespace.
        /// </remarks>
        public TopicSubscription? TopicSubscription { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum concurrent calls to process messages.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
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
        /// Gets or sets the indication whether or not messages should be automatically marked as completed if no exceptions occurred and processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed.</remarks>
        public bool AutoComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets the flag to indicate whether or not to emit security events during the lifetime of the message pump.
        /// </summary>
        public bool EmitSecurityEvents { get; set; } = false;

        /// <summary>
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string JobId
        {
            get => _jobId;
            set
            {
                Guard.NotNullOrEmpty(value, nameof(value), "Unique identifier for background job cannot be empty");
                _jobId = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout when the message pump tries to restart and re-authenticate during key rotation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than <see cref="TimeSpan.Zero"/>.</exception>
        public TimeSpan KeyRotationTimeout
        {
            get => _keyRotationTimeout;
            set
            {
                Guard.NotLessThan(value, TimeSpan.Zero, nameof(value), "Key rotation timeout cannot be less than a zero time range");
                _keyRotationTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the fallback when the Azure Key Vault notification doesn't get delivered correctly,
        /// how many times should the message pump run into an <see cref="UnauthorizedAccessException"/> before restarting.
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

        /// <summary>
        /// Gets the options to control the correlation information upon the receiving of Azure Service Bus messages in the <see cref="AzureServiceBusMessagePump"/>.
        /// </summary>
        public AzureServiceBusCorrelationOptions Correlation { get; } = new AzureServiceBusCorrelationOptions();

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus Queue message pumps.
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions DefaultQueueOptions { get; } = new AzureServiceBusMessagePumpOptions()
        {
            TopicSubscription = null
        };

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus Topic message pumps.
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions DefaultTopicOptions { get; } = new AzureServiceBusMessagePumpOptions()
        {
            TopicSubscription = ServiceBus.TopicSubscription.CreateOnStart | ServiceBus.TopicSubscription.DeleteOnStop
        };
    }
}