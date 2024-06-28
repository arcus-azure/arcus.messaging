using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
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

            var options = new MessagePumpCircuitBreakerOptions();
            configureOptions?.Invoke(options);

            MessagePump messagePump = GetRegisteredMessagePump(jobId);
            if (messagePump.IsStarted)
            {
                _logger.LogTrace("Open circuit by pausing message processing for message pump '{JobId}'...", jobId);
                await messagePump.StopProcessingMessagesAsync(CancellationToken.None);

                await Task.Factory.StartNew(async () =>
                {
                    await WaitRecoveryTimeAsync(messagePump, options);
                    await TryProcessSingleMessageAsync(messagePump, options);
                }, TaskCreationOptions.LongRunning);
                
            }
            else
            {
                await Task.Factory.StartNew(async () =>
                {
                    await WaitMessageIntervalAsync(messagePump, options);
                    await TryProcessSingleMessageAsync(messagePump, options);
                }, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        /// Continue the process of receiving messages in the message pump after a successful message handling.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public virtual async Task ResumeMessageProcessingAsync(string jobId)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

            MessagePump messagePump = GetRegisteredMessagePump(jobId);
            if (!messagePump.IsStarted)
            {
                _logger.LogTrace("Message pump '{JobId}' successfully handled a single message, closing circuit...", messagePump.JobId);
                await messagePump.StartProcessingMessagesAsync(CancellationToken.None);
            }
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

        private async Task TryProcessSingleMessageAsync(MessagePump messagePump, MessagePumpCircuitBreakerOptions options)
        {
            _logger.LogTrace("Try to process single message in message pump '{JobId}'...", messagePump.JobId);
            await messagePump.TryProcessProcessSingleMessageAsync(options);
        }

        private async Task WaitMessageIntervalAsync(MessagePump messagePump, MessagePumpCircuitBreakerOptions options)
        {
            _logger.LogError("Message pump '{JobId}' failed to handle a single message, wait configured interval period ({IntervalPeriod}) before retrying...", messagePump.JobId, options.MessageIntervalDuringRecovery);
            await Task.Delay(options.MessageIntervalDuringRecovery);
        }

        private async Task WaitRecoveryTimeAsync(MessagePump messagePump, MessagePumpCircuitBreakerOptions options)
        {
            _logger.LogTrace("Wait configured recovery period ({RecoveryPeriod}) before trying to close circuit for message pump '{JobId}'", options.MessageRecoveryPeriod, messagePump.JobId);
            await Task.Delay(options.MessageRecoveryPeriod);
        }
    }
}