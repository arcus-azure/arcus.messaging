using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// The general options that configures a <see cref="ServiceBusReceiverMessagePump"/> implementation.
    /// </summary>
    public class ServiceBusMessagePumpOptions
    {
        private int _maxMessagesPerBatch = 1;
        private int _prefetchCount = 0;
        private string _jobId = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the maximum concurrent calls to process messages.
        /// </summary>
        /// <remarks>The default value is 1</remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        [Obsolete("Will be removed in v4, use " + nameof(MaxMessagesPerBatch) + " instead", DiagnosticId = "ARCUS")]
        public int MaxConcurrentCalls
        {
            get => MaxMessagesPerBatch;
            set => MaxMessagesPerBatch = value;
        }

        /// <summary>
        /// Gets the amount of messages that will be retrieved in one batch from Azure Service Bus.
        /// </summary>
        /// <remarks>The default value is 1.</remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        public int MaxMessagesPerBatch
        {
            get => _maxMessagesPerBatch;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
                _maxMessagesPerBatch = value;
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
        /// Gets the options related to manipulating the lifecycle of the message pump in the application.
        /// </summary>
        public ServiceBusMessagePumpHooksOptions Hooks { get; } = new();

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        public AzureServiceBusMessageRouterOptions Routing { get; } = new();

        internal ServiceBusSessionOptions Session { get; } = new();
        internal bool RequestedToUseSessions { get; private set; }

        /// <summary>
        /// Activates the session-aware message pump that processes messages in the Azure Service Bus
        /// with additional configuration options for session handling.
        /// </summary>
        public void UseSessions()
        {
            UseSessions(configureSessionOptions: null);
        }

        /// <summary>
        /// Activates the session-aware message pump that processes messages in the Azure Service Bus
        /// with additional configuration options for session handling.
        /// </summary>
        /// <param name="configureSessionOptions">The function to manipulate how sessions should be handled by the pump.</param>
        public void UseSessions(Action<ServiceBusSessionOptions> configureSessionOptions)
        {
            RequestedToUseSessions = true;
            configureSessionOptions?.Invoke(Session);
        }

        /// <summary>
        /// Gets the consumer configurable options model to change the behavior of the tracked Azure Service bus request telemetry.
        /// </summary>
        public MessageTelemetryOptions Telemetry { get; } = new()
        {
            OperationName = "Azure Service Bus message processing"
        };
    }

    /// <summary>
    /// Represents the available options to configure a session-aware message pump that processes messages in the Azure Service Bus.
    /// </summary>
    public class ServiceBusSessionOptions
    {
        private int _maxConcurrentCallsPerSession = 1;
        private int _maxConcurrentSessions = 8;
        private TimeSpan _sessionIdleTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// <para>Gets or sets the maximum number of calls to the callback the processor will initiate per session.</para>
        /// <para>Total number of callbacks = <see cref="MaxConcurrentSessions"/> x <see cref="MaxConcurrentCallsPerSession"/>.</para>
        /// </summary>
        /// <remarks>The default value is 1.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        public int MaxConcurrentCallsPerSession
        {
            get => _maxConcurrentCallsPerSession;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
                _maxConcurrentCallsPerSession = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the maximum number of sessions that will be processed concurrently by the processor.</para>
        /// <para>Total number of callbacks = <see cref="MaxConcurrentSessions"/> x <see cref="MaxConcurrentCallsPerSession"/>.</para>
        /// </summary>
        /// <remarks>The default value is 8.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        public int MaxConcurrentSessions
        {
            get => _maxConcurrentSessions;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
                _maxConcurrentSessions = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of time to wait for a message to be received for the currently active session.
        /// After this time has elapsed, the processor will close the session and attempt to process another session.
        /// </summary>
        /// <remarks>The default value is 1 minute.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than <see cref="TimeSpan.Zero"/>.</exception>
        public TimeSpan SessionIdleTimeout
        {
            get => _sessionIdleTimeout;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
                _sessionIdleTimeout = value;
            }
        }
    }

    /// <summary>
    /// Represents the options related to the <see cref="ServiceBusMessagePumpOptions.Hooks"/> of the registered Azure Service Bus message pump.
    /// </summary>
    public class ServiceBusMessagePumpHooksOptions
    {
        internal Func<IServiceProvider, Task> BeforeStartupAsync { get; set; } = _ => Task.CompletedTask;

        /// <summary>
        /// Sets a function on the message pump that should run before the pump receives Azure Service Bus messages.
        /// Useful for when dependent systems are not always directly available.
        /// </summary>
        /// <remarks>
        ///     ⚠️ Multiple calls will override each other.
        /// </remarks>
        /// <param name="beforeStartupAsync">The function that upon completion 'triggers' the message pump to be started.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="beforeStartupAsync"/> is <c>null</c>.</exception>
        public void BeforeStartup(Func<IServiceProvider, Task> beforeStartupAsync)
        {
            ArgumentNullException.ThrowIfNull(beforeStartupAsync);
            BeforeStartupAsync = beforeStartupAsync;
        }
    }
}