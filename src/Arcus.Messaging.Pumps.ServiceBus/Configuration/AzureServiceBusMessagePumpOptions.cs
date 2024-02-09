using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using GuardNet;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// The general options that configures a <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    public class AzureServiceBusMessagePumpOptions : IAzureServiceBusQueueMessagePumpOptions, IAzureServiceBusTopicMessagePumpOptions
    {
        private int _maxConcurrentCalls = 1;
        private int _prefetchCount = 0;
        private string _jobId = Guid.NewGuid().ToString();
        private TimeSpan _keyRotationTimeout = TimeSpan.FromSeconds(5);
        private int _maximumUnauthorizedExceptionsBeforeRestart = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpOptions"/> class.
        /// </summary>
        public AzureServiceBusMessagePumpOptions()
        {
            Routing = new AzureServiceBusMessageRouterOptions();
#pragma warning disable CS0618
            Deserialization = Routing.Deserialization;
            Correlation = new AzureServiceBusCorrelationOptions(Routing.Correlation);
#pragma warning restore CS0618
        }

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
        /// <remarks>The default value is 1</remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        public int MaxConcurrentCalls
        {
            get => _maxConcurrentCalls;
            set
            {
                Guard.For<ArgumentException>(() => value <= 0, "Max concurrent calls has to be 1 or above.");

                _maxConcurrentCalls = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of messages that will be eagerly requested from
        /// Queues or Subscriptions and queued locally, intended to help maximize throughput
        /// by allowing the processor to receive from a local cache rather than waiting on a service request.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        /// <remarks>The default value is 0.</remarks>
        public int PrefetchCount
        {
            get => _prefetchCount;
            set
            {
                Guard.For<ArgumentException>(() => value < 0, "PrefetchCount has to be 0 or above.");
                _prefetchCount = value;
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
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank job identifier for the Azure Service Bus message pump");
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
        [Obsolete("Use the " + nameof(Routing) + "." + nameof(AzureServiceBusMessageRouterOptions.Correlation) + " instead")]
        public AzureServiceBusCorrelationOptions Correlation { get; }

        /// <summary>
        /// Gets the consumer-configurable options to change the deserialization behavior.
        /// </summary>
        [Obsolete("Use the " + nameof(Routing) + "." + nameof(AzureServiceBusMessageRouterOptions.Deserialization) + " instead")]
        public MessageDeserializationOptions Deserialization { get; }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        public AzureServiceBusMessageRouterOptions Routing { get; }

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus Queue message pumps.
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions DefaultQueueOptions => new AzureServiceBusMessagePumpOptions()
        {
            TopicSubscription = null
        };

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus Topic message pumps.
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions DefaultTopicOptions => new AzureServiceBusMessagePumpOptions()
        {
            TopicSubscription = ServiceBus.TopicSubscription.None
        };
    }
}