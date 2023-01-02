using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    /// Represents the default <see cref="IMessagePumpLifetime"/> implementation to control the lifetime of registered message pumps.
    /// </summary>
    public class DefaultMessagePumpLifetime : IMessagePumpLifetime
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagePumpLifetime" /> class.
        /// </summary>
        /// <param name="serviceProvider">The application service provider where the message pumps are registered.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public DefaultMessagePumpLifetime(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider instance to retrieve the registered message pumps");
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILogger<DefaultMessagePumpLifetime>>() ?? NullLogger<DefaultMessagePumpLifetime>.Instance;
        }

        /// <summary>
        /// Starts a message pump with the given <paramref name="jobId"/>.
        /// </summary>
        /// <param name="jobId">The uniquely defined identifier of the registered message pump.</param>
        /// <param name="cancellationToken">The token to indicate that the start process has been aborted.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public async Task PauseProcessingMessagesAsync(string jobId, CancellationToken cancellationToken)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires a message pump job ID to retrieve the registered message pump in the application services");

            MessagePump messagePump = GetMessagePump(jobId);
            
            _logger.LogTrace("Starting message pump '{JobId}' via message pump lifetime...", jobId);
            await messagePump.StartProcessingMessagesAsync(cancellationToken);
            _logger.LogTrace("Started message pump '{JobId}' via message pump lifetime", jobId);
        }

        /// <summary>
        /// Pauses a message pump with the given <paramref name="jobId"/> for a specified <paramref name="duration"/>.
        /// </summary>
        /// <param name="jobId">The uniquely defined identifier of the registered message pump.</param>
        /// <param name="duration">The time duration in which the message pump should be stopped.</param>
        /// <param name="cancellationToken">The token to indicate that the shutdown and start process should no longer be graceful.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        public async Task PauseProcessingMessagesAsync(string jobId, TimeSpan duration, CancellationToken cancellationToken)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires a message pump job ID to retrieve the registered message pump in the application services");

            MessagePump messagePump = GetMessagePump(jobId);
            _logger.LogTrace("Stopping message pump '{JobId}' via message pump lifetime...", jobId);
            await messagePump.StopProcessingMessagesAsync(cancellationToken);
            _logger.LogTrace("Stopped message pump '{JobId}' via message pump lifetime", jobId);

            _ = Task.Delay(duration, cancellationToken)
                    .ContinueWith(async t =>
                    {
                        _logger.LogTrace("Starting message pump '{JobId}' via message pump lifetime...", jobId);
                        await messagePump.StartProcessingMessagesAsync(cancellationToken);
                        _logger.LogTrace("Started message pump '{JobId}' via message pump lifetime", jobId);
                    }, cancellationToken);
        }

        /// <summary>
        /// Stops a message pump with a given <paramref name="jobId"/>.
        /// </summary>
        /// <param name="jobId">The uniquely defined identifier of the registered message pump.</param>
        /// <param name="cancellationToken">The token to indicate that the shutdown process should no longer be graceful.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public async Task StopProcessingMessagesAsync(string jobId, CancellationToken cancellationToken)
        {
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires a message pump job ID to retrieve the registered message pump in the application services");

            MessagePump messagePump = GetMessagePump(jobId);

            _logger.LogTrace("Stopping message pump '{JobId}' via message pump lifetime...", jobId);
            await messagePump.StopProcessingMessagesAsync(cancellationToken);
            _logger.LogTrace("Stopped message pump '{JobId}' via message pump lifetime", jobId);
        }

        private MessagePump GetMessagePump(string jobId)
        {
            _logger.LogTrace("Getting message pump '{JobId}' in application services...", jobId);
            MessagePump messagePump =
                _serviceProvider.GetServices<IHostedService>()
                                .OfType<MessagePump>()
                                .FirstOrDefault(pump => pump.JobId == jobId);

            if (messagePump is null)
            {
                throw new InvalidOperationException(
                    $"Cannot control lifetime of Arcus message pump with ID '{jobId}' because no message pump was found in the registered application services");
            }

            _logger.LogTrace("Get message pump '{JobId}' in application services", jobId);
            return messagePump;
        }
    }
}