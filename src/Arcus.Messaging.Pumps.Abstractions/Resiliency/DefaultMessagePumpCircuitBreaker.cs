using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Pumps.Abstractions.Resiliency
{
    /// <summary>
    /// Represents a default implementation of the <see cref="IMessagePumpCircuitBreaker"/>
    /// that starts and stops a configured message pump by its configured <see cref="MessagePumpCircuitBreakerOptions"/>.
    /// </summary>
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
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
            _logger = logger ?? NullLogger<DefaultMessagePumpCircuitBreaker>.Instance;
        }

        /// <summary>
        /// Pause the process of receiving messages in the message pump for a period of time before careful retrying again.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <param name="configureOptions">The optional user-configurable options to manipulate the workings of the message pump interaction.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public virtual async Task PauseMessageProcessingAsync(string jobId, Action<MessagePumpCircuitBreakerOptions> configureOptions)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

            MessagePump messagePump = GetRegisteredMessagePump(jobId);

            if (!messagePump.IsStarted)
            {
                _logger.LogWarning($"Cannot pause MessagePump for JobId {jobId} because the MessagePump has not been started.");
                return;
            }

            if (messagePump.CircuitState != MessagePumpCircuitState.Closed)
            {
                _logger.LogWarning($"Cannot pause MessagePump for JobId {jobId} because the MessagePump's circuitbreaker is not in a closed state.");
                return;
            }

            var options = new MessagePumpCircuitBreakerOptions();
            configureOptions?.Invoke(options);

            _logger.LogDebug("Open circuit by pausing message processing for message pump '{JobId}'...", jobId);

            await messagePump.StopProcessingMessagesAsync(CancellationToken.None);

            await Task.Factory.StartNew(async () =>
            {
                await WaitRecoveryTimeAsync(messagePump, options);
                _logger.LogWarning("recovery Waittime passed");
                MessageProcessingResult result;
                do
                {
                    try
                    {
                        result = await TryProcessSingleMessageAsync(messagePump, options);
                    }
                    catch (Exception ex)
                    {
                        result = MessageProcessingResult.Failure(ex);
                    }

                    _logger.LogInformation("Single message processed with succesfull result: " + result.IsSuccessful);

                    if (!result.IsSuccessful)
                    {
                        await WaitMessageIntervalDuringRecoveryAsync(messagePump, options);
                    }

                } while (!result.IsSuccessful);

                await ResumeMessageProcessingAsync(jobId);

            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Continue the process of receiving messages in the message pump after a successful message handling.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        private async Task ResumeMessageProcessingAsync(string jobId)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

            MessagePump messagePump = GetRegisteredMessagePump(jobId);

            if (messagePump.IsStarted)
            {
                _logger.LogWarning("Resume called on Message pump '{JobId}' but Message pump is already started. CircuitState = {CircuitState}", jobId, messagePump.CircuitState);
                return;
            }

            _logger.LogInformation("Message pump '{JobId}' successfully handled a single message, resume message processing (circuit breaker: closed)", messagePump.JobId);
            await messagePump.StartProcessingMessagesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Gets the current circuit breaker state of message processing in the given message pump.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public MessagePumpCircuitState GetCircuitBreakerState(string jobId)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

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
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

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

        private async Task<MessageProcessingResult> TryProcessSingleMessageAsync(MessagePump messagePump, MessagePumpCircuitBreakerOptions options)
        {
            _logger.LogDebug("Try to process single message in message pump '{JobId}' (state: half-open)", messagePump.JobId);
            var result = await messagePump.TryProcessProcessSingleMessageAsync(options);

            if (result.IsSuccessful == false)
            {
                _logger.LogWarning("failed process single message");
            }

            return result;
        }

        private async Task WaitMessageIntervalDuringRecoveryAsync(MessagePump messagePump, MessagePumpCircuitBreakerOptions options)
        {
            _logger.LogDebug("Wait configured interval period ({IntervalPeriod}) since message pump '{JobId}' failed to handle a single message (circuit breaker: open)", options.MessageIntervalDuringRecovery, messagePump.JobId);
            await Task.Delay(options.MessageIntervalDuringRecovery);
        }

        private async Task WaitRecoveryTimeAsync(MessagePump messagePump, MessagePumpCircuitBreakerOptions options)
        {
            _logger.LogDebug("Wait configured recovery period ({RecoveryPeriod}) since message pump '{JobId}' failed to process messages (circuit breaker: open)", options.MessageRecoveryPeriod, messagePump.JobId);
            await Task.Delay(options.MessageRecoveryPeriod);
        }
    }
}