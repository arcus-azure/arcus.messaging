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
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpOptions"/> class.
        /// </summary>
        public AzureServiceBusMessagePumpOptions()
        {
            Routing = new AzureServiceBusMessageRouterOptions();
        }

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
        public bool AutoComplete { get; set; } = true;

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
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        public AzureServiceBusMessageRouterOptions Routing { get; }

        /// <summary>
        /// Gets the consumer configurable options model to change the behavior of the tracked Azure Service bus request telemetry.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public MessageTelemetryOptions Telemetry => Routing.Telemetry;
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus Queue message pumps.
        /// </summary>
        internal static AzureServiceBusMessagePumpOptions DefaultOptions => new();
    }
}