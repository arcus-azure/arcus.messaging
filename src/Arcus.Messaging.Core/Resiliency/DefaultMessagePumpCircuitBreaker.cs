using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.Abstractions.Resiliency
{
    /// <summary>
    /// Represents a default implementation of the <see cref="IMessagePumpCircuitBreaker"/>
    /// that starts and stops a configured message pump by its configured <see cref="MessagePumpCircuitBreakerOptions"/>.
    /// </summary>
    [Obsolete("Will be removed in v4.0, please use implementations of the " + nameof(IMessagePumpCircuitBreaker) + " directly available with the concrete message pumps")]
    public class DefaultMessagePumpCircuitBreaker : IMessagePumpCircuitBreaker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagePumpCircuitBreaker" /> class.
        /// </summary>
        /// <param name="serviceProvider">The application services to retrieve the registered <see cref="MessagePump"/>.</param>
        /// <param name="logger">The logger instance to write diagnostic messages during the inspection of healthy message pumps.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public DefaultMessagePumpCircuitBreaker(IServiceProvider serviceProvider, ILogger<DefaultMessagePumpCircuitBreaker> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? NullLogger<DefaultMessagePumpCircuitBreaker>.Instance;
        }

        /// <summary>
        /// Pause the process of receiving messages in the message pump for a period of time before careful retrying again.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <param name="configureOptions">The optional user-configurable options to manipulate the workings of the message pump interaction.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public virtual Task PauseMessageProcessingAsync(string jobId, Action<MessagePumpCircuitBreakerOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank unique job ID to identify he message pump", nameof(jobId));
            }

            MessagePump messagePump = GetRegisteredMessagePump(jobId);

            if (!messagePump.IsStarted)
            {
                _logger.LogWarning("Cannot pause message pump '{JobId}' because the pump has not been started", jobId);
                return Task.CompletedTask;
            }

            if (!messagePump.CircuitState.IsClosed)
            {
                return Task.CompletedTask;
            }

            var options = new MessagePumpCircuitBreakerOptions();
            configureOptions?.Invoke(options);

            messagePump.NotifyPauseReceiveMessages(options);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the current circuit breaker state of message processing in the given message pump.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public MessagePumpCircuitState GetCircuitBreakerState(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank unique job ID to identify he message pump", nameof(jobId));
            }

            MessagePump messagePump = GetRegisteredMessagePump(jobId);
            return messagePump.CircuitState;
        }

        /// <summary>
        /// Get the registered <see cref="MessagePump"/> from the application services
        /// for which to pause the process of receiving messages.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when not a single or more than one message pump could be found by the configured job ID.</exception>
        protected MessagePump GetRegisteredMessagePump(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank unique job ID to identify he message pump", nameof(jobId));
            }

            MessagePump[] messagePumps =
                _serviceProvider.GetServices<IHostedService>()
                         .OfType<MessagePump>()
                         .Where(p => p.JobId == jobId)
                         .ToArray();

            if (messagePumps.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot find one correct registered message pump with job ID: '{jobId}', please make sure to register a single message pump instance in the application services with this job ID");
            }

            if (messagePumps.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Cannot find one correct registered message pump as multiple pump instances were registered with the same job ID: '{jobId}', please make sure to only register a single message pump instance in the application services with this job ID");
            }

            return messagePumps[0];
        }
    }
}