using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    /// Represents the foundation for building message pumps.
    /// </summary>
    public abstract class MessagePump : BackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePump"/> class.
        /// </summary>
        /// <param name="configuration">The configuration of the application.</param>
        /// <param name="serviceProvider">The collection of services that are configured.</param>
        /// <param name="logger">The logger to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, the <paramref name="serviceProvider"/>, or <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        protected MessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(serviceProvider, nameof(serviceProvider));
            Guard.NotNull(logger, nameof(logger));

            Logger = logger;
            Configuration = configuration;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; protected set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the boolean flag that indicates whether the message pump is started.
        /// </summary>
        public bool IsStarted { get; protected set; }

        /// <summary>
        /// Gets the current state of the message pump within the circuit breaker context.
        /// </summary>
        public MessagePumpCircuitState CircuitState { get; private set; } = MessagePumpCircuitState.Closed;

        /// <summary>
        /// Gets hte ID of the client being used to connect to the messaging service.
        /// </summary>
        protected string ClientId { get; private set; }

        /// <summary>
        /// Gets entity path that is being processed.
        /// </summary>
        public string EntityPath { get; private set; }

        /// <summary>
        /// Gets the configuration of the application.
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the collection of application services that are configured.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the default encoding used during the message processing through the message pump.
        /// </summary>
        protected Encoding DefaultEncoding { get; } = Encoding.UTF8;

        /// <summary>
        /// Gets the logger to write telemetry to.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Handles an exception that occurred during the receiving of a message
        /// </summary>
        /// <param name="receiveException">Exception that occurred</param>
        protected virtual Task HandleReceiveExceptionAsync(Exception receiveException)
        {
            Logger.LogCritical(receiveException, "Unable to process message from {EntityPath} with client {ClientId}: {Message}", EntityPath, ClientId, receiveException.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogWarning("Host is shutting down");

            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Start with receiving messages on this message pump.
        /// </summary>
        /// <param name="cancellationToken">The token to indicate the start process should no longer be graceful.</param>
        public virtual Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            IsStarted = true;
            CircuitState = MessagePumpCircuitState.Closed;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop with receiving messages on this message pump.
        /// </summary>
        /// <param name="cancellationToken">The token to indicate the stop process should no longer be graceful.</param>
        public virtual Task StopProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            IsStarted = false;
            CircuitState = CircuitState.TransitionTo(CircuitBreakerState.Open);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Waits a previously configured amount of time until the message pump is expected to be recovered (Closed to Open state).
        /// </summary>
        /// <param name="cancellationToken">The token to cancel the wait period.</param>
        protected async Task WaitMessageRecoveryPeriodAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Circuit breaker caused message pump '{JobId}' to wait message recovery period of '{Recovery}' during '{State}' state", JobId, CircuitState.Options.MessageRecoveryPeriod.ToString("g"), CircuitState);
            await Task.Delay(CircuitState.Options.MessageRecoveryPeriod, cancellationToken);

            if (!CircuitState.IsHalfOpen)
            {
                CircuitState = CircuitState.TransitionTo(CircuitBreakerState.HalfOpen);

                NotifyCircuitBreakerEventHandlers();
            }
        }

        /// <summary>
        /// Waits a previously configured amount of time until the next single message can be tried (Half-Open state).
        /// </summary>
        /// <param name="cancellationToken">The token to cancel the wait period.</param>
        protected async Task WaitMessageIntervalDuringRecoveryAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Circuit breaker caused message pump '{JobId}' to wait message interval during recovery of '{Interval}' during the '{State}' state", JobId, CircuitState.Options.MessageIntervalDuringRecovery.ToString("g"), CircuitState);
            await Task.Delay(CircuitState.Options.MessageIntervalDuringRecovery, cancellationToken);

            if (!CircuitState.IsHalfOpen)
            {
                CircuitState = CircuitState.TransitionTo(CircuitBreakerState.HalfOpen);

                NotifyCircuitBreakerEventHandlers();
            }
        }

        /// <summary>
        /// Notifies the message pump about the new state which pauses message retrieval.
        /// </summary>
        /// <param name="options">The additional accompanied options that goes with the new state.</param>
        internal void NotifyPauseReceiveMessages(MessagePumpCircuitBreakerOptions options)
        {
            Logger.LogDebug("Circuit breaker caused message pump '{JobId}' to transition from a '{CurrentState}' an 'Open' state", JobId, CircuitState);

            CircuitState = CircuitState.TransitionTo(CircuitBreakerState.Open, options);

            NotifyCircuitBreakerEventHandlers();
        }

        /// <summary>
        /// Notifies the message pump about the new state which resumes message retrieval.
        /// </summary>
        protected void NotifyResumeRetrievingMessages()
        {
            Logger.LogDebug("Circuit breaker caused message pump '{JobId}' to transition back from '{CurrentState}' to a 'Closed' state, retrieving messages is resumed", JobId, CircuitState);

            CircuitState = MessagePumpCircuitState.Closed;

            NotifyCircuitBreakerEventHandlers();
        }

        private void NotifyCircuitBreakerEventHandlers()
        {
            ICircuitBreakerEventHandler[] eventHandlers = GetEventHandlersForPump();

            foreach (var handler in eventHandlers)
            {
                Task.Run(() => handler.OnTransition(CircuitState));
            }
        }

        private ICircuitBreakerEventHandler[] GetEventHandlersForPump()
        {
            return ServiceProvider.GetServices<CircuitBreakerEventHandler>()
                                  .Where(registration => registration.JobId == JobId)
                                  .Select(handler => handler.Handler)
                                  .ToArray();
        }

        /// <summary>
        /// Register information about the client connected to the messaging service
        /// </summary>
        /// <param name="clientId">Id of the client being used to connect to the messaging service</param>
        /// <param name="entityPath">Entity path that is being processed</param>
        protected void RegisterClientInformation(string clientId, string entityPath)
        {
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));

            ClientId = clientId;
            EntityPath = entityPath;
        }
    }
}