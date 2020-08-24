using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.KeyVault.Events;
using Arcus.Messaging.Abstractions;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;

namespace Arcus.Messaging.Pumps.ServiceBus.KeyRotation
{
    public class ReAuthenticateMessageHandler : IAzureServiceBusMessageHandler<CloudEvent>
    {
        private readonly AzureServiceBusMessagePump _messagePump;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReAuthenticateMessageHandler"/> class.
        /// </summary>
        public ReAuthenticateMessageHandler(AzureServiceBusMessagePump messagePump, ILogger<ReAuthenticateMessageHandler> logger)
        {
            Guard.NotNull(messagePump, nameof(messagePump));
            Guard.NotNull(logger, nameof(logger));

            _messagePump = messagePump;
            _logger = logger;
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            CloudEvent message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Cannot invalidate Azure KeyVault secret from a 'null' CloudEvent");

            _logger.LogTrace("Receiving new Azure Key Vault notification...");
            
            var secretNewVersionCreated = message.GetPayload<SecretNewVersionCreated>();
            if (secretNewVersionCreated is null)
            {
                throw new CloudException(
                    "Azure Key Vault job cannot map Event Grid event to CloudEvent because the event data isn't recognized as a 'SecretNewVersionCreated' schema");
            }

            _logger.LogInformation("Received Azure Key vault 'Secret New Version Created' event");
            
            await _messagePump.RestartAsync();
        }
    }
}
