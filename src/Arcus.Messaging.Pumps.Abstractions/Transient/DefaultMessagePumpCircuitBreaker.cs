using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.Messaging.Pumps.Abstractions.Transient
{
    /// <summary>
    /// Represents a default implementation of the <see cref="IMessagePumpCircuitBreaker"/>
    /// that starts and stops a configured message pump by its configured <see cref="MessagePumpCircuitBreakerOptions"/>.
    /// </summary>
    public class DefaultMessagePumpCircuitBreaker : IMessagePumpCircuitBreaker
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagePumpCircuitBreaker" /> class.
        /// </summary>
        /// <param name="serviceProvider">The application services to retrieve the registered <see cref="MessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public DefaultMessagePumpCircuitBreaker(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Pause the process of receiving messages in the message pump for a period of time before careful retrying again.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <param name="configureOptions">The optional user-configurable options to manipulate the workings of the message pump interaction.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public virtual async Task PauseMessageProcessingAsync(string jobId,  Action<MessagePumpCircuitBreakerOptions> configureOptions)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

            var options = new MessagePumpCircuitBreakerOptions();
            configureOptions?.Invoke(options);

            MessagePump messagePump = GetRegisteredMessagePump(jobId);
            await messagePump.StopProcessingMessagesAsync(CancellationToken.None);

            await WaitUntilRecoveredAsync(messagePump, options);
            await messagePump.StartProcessingMessagesAsync(CancellationToken.None);
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

            return messagePumps.First();
        }

        private static async Task WaitUntilRecoveredAsync(MessagePump messagePump, MessagePumpCircuitBreakerOptions options)
        {
            await Task.Delay(options.MessageRecoveryPeriod);

            bool isRecovered = false;
            while (!isRecovered)
            {
                isRecovered = await messagePump.TryProcessProcessSingleMessageAsync(options);
                if (!isRecovered)
                {
                    await Task.Delay(options.MessageIntervalDuringRecovery);
                }
            }
        }
    }
}