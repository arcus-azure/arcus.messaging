using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// The general options that configures a <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    public class AzureServiceBusMessagePumpOptions
    {
        private int _maxConcurrentCalls = 1;
        private int _prefetchCount = 0;
        private string _jobId = Guid.NewGuid().ToString();

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
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
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
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
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
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string JobId
        {
            get => _jobId;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _jobId = value;
            }
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        public AzureServiceBusMessageRouterOptions Routing { get; } = new();

        /// <summary>
        /// Gets the consumer configurable options model to change the behavior of the tracked Azure Service bus request telemetry.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public MessageTelemetryOptions Telemetry => Routing.Telemetry;
#pragma warning restore CS0618 // Type or member is obsolete
    }
}