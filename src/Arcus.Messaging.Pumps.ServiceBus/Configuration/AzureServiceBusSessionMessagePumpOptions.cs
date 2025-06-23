using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// The general options for configuring a <see cref="AzureServiceBusSessionMessagePump"/>.
    /// </summary>
    public class AzureServiceBusSessionMessagePumpOptions //: AzureServiceBusMessagePumpOptions
    {
        /// <summary>
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string JobId { get; private set; }

        /// <summary>
        /// Gets or sets the indication whether or not messages should be automatically marked as completed if no exceptions occurred and processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed.</remarks>
        public bool AutoComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of messages that will be eagerly requested from
        /// Queues or Subscriptions and queued locally, intended to help maximize throughput
        /// by allowing the processor to receive from a local cache rather than waiting on a service request.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        /// <remarks>The default value is 0.</remarks>
        public int PrefetchCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the maximum number of calls to the callback the processor will initiate per session.
        /// Thus the total number of callbacks will be equal to MaxConcurrentSessions * MaxConcurrentCallsPerSession. The default value is 1.
        /// </summary>
        public int MaxConcurrentCallsPerSession { get; private set; }

        /// <summary>
        /// Gets the maximum number of sessions that will be processed concurrently by the processor. The default value is 8.
        /// </summary>
        public int MaxConcurrentSessions { get; private set; }

        /// <summary>
        /// Gets the maximum amount of time to wait for a message to be received for the currently active session. After this time has elapsed, the processor will close the session and attempt to process another session.
        /// The default value is 60 seconds.
        /// </summary>
        public TimeSpan SessionIdleTimeout { get; private set; }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        public AzureServiceBusMessageRouterOptions Routing { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusSessionMessagePumpOptions"/> class.
        /// </summary>
        public AzureServiceBusSessionMessagePumpOptions()
        {
            JobId = Guid.NewGuid().ToString();
            PrefetchCount = 0;
            MaxConcurrentCallsPerSession = 1;
            MaxConcurrentSessions = 1;
            SessionIdleTimeout = TimeSpan.FromSeconds(60);
            Routing = new AzureServiceBusMessageRouterOptions();
        }

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus message pumps that have Session support.
        /// </summary>
        public static readonly AzureServiceBusSessionMessagePumpOptions DefaultOptions = new AzureServiceBusSessionMessagePumpOptions();
    }
}
