using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

#pragma warning disable CS0618

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
        }

        /// <summary>
        /// <para>Gets or sets the value indicating whether or not a new Azure Service Bus Topic subscription has to be created when the <see cref="AzureServiceBusMessagePump"/> starts.</para>
        /// <para>The subscription will be deleted afterwards when the message pump stops if the options <see cref="ServiceBus.TopicSubscription.Automatic"/> is selected.</para>
        /// </summary>
        /// <remarks>
        ///     Provides capability to create and delete these subscriptions. This requires 'Manage' permissions on the Azure Service Bus Topic or namespace.
        /// </remarks>
        [Obsolete("Will be removed in v3.0 as automatic Azure Service bus topic subscription creation and deletion will not be supported anymore")]
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
                if (value <= 0)
                {
                    throw new ArgumentException("Max concurrent calls has to be 1 or above", nameof(value));
                }

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
                if (value < 0)
                {
                    throw new ArgumentException("PrefetchCount has to be 0 or above", nameof(value));
                }

                _prefetchCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the indication whether or not messages should be automatically marked as completed if no exceptions occurred and processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed.</remarks>
        [Obsolete("Will be removed in v4.0, please use " + nameof(Routing.AutoComplete) + " instead")]
        public bool AutoComplete
        {
            get => Routing.AutoComplete;
            set => Routing.AutoComplete = value;
        }

        /// <summary>
        /// Gets or sets the flag to indicate whether or not to emit security events during the lifetime of the message pump.
        /// </summary>
        [Obsolete("Will be removed in v3.0 as the direct link to Arcus.Observability will be removed as well")]
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
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank job identifier for the Azure Service Bus message pump", nameof(value));
                }

                _jobId = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout when the message pump tries to restart and re-authenticate during key rotation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than <see cref="TimeSpan.Zero"/>.</exception>
        [Obsolete("Will be removed in v3.0 as the key rotation functionality will be removed as well")]
        public TimeSpan KeyRotationTimeout
        {
            get => _keyRotationTimeout;
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Key rotation timeout cannot be less than a zero time range");
                }

                _keyRotationTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the fallback when the Azure Key Vault notification doesn't get delivered correctly,
        /// how many times should the message pump run into an <see cref="UnauthorizedAccessException"/> before restarting.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        [Obsolete("Will be removed in v3.0 as the Azure Service Bus authentication has been moved outside of the message pump's responsibility")]
        public int MaximumUnauthorizedExceptionsBeforeRestart
        {
            get => _maximumUnauthorizedExceptionsBeforeRestart;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Requires an unauthorized exceptions count that's greater than zero");
                }

                _maximumUnauthorizedExceptionsBeforeRestart = value;
            }
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        public AzureServiceBusMessageRouterOptions Routing { get; }

        /// <summary>
        /// Gets the consumer configurable options model to change the behavior of the tracked Azure Service bus request telemetry.
        /// </summary>
        public MessageTelemetryOptions Telemetry => Routing.Telemetry;

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus Queue message pumps.
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions DefaultOptions => new AzureServiceBusMessagePumpOptions()
        {
            TopicSubscription = ServiceBus.TopicSubscription.None
        };
    }
}