using System;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// The general options for configuring a <see cref="AzureServiceBusSessionMessagePump"/>.
    /// </summary>
    public class AzureServiceBusSessionMessagePumpOptions : AzureServiceBusMessagePumpOptions
    {
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
        /// Initializes a new instance of the <see cref="AzureServiceBusSessionMessagePumpOptions"/> class.
        /// </summary>
        public AzureServiceBusSessionMessagePumpOptions()
        {
            MaxConcurrentCallsPerSession = 1;
            MaxConcurrentSessions = 1;
            SessionIdleTimeout = TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// Gets the default consumer-configurable options for Azure Service Bus message pumps that have Session support.
        /// </summary>
        public static new readonly AzureServiceBusSessionMessagePumpOptions DefaultOptions = new AzureServiceBusSessionMessagePumpOptions();
    }
}
