using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.KeyVault.Events;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest.Azure;

namespace Arcus.Messaging.Pumps.ServiceBus.KeyRotation
{
    /// <summary>
    /// Represents an <see cref="IMessageHandler{TMessage,TMessageContext}"/> that processes <see cref="CloudEvent"/>s representing <see cref="SecretNewVersionCreated"/> events
    /// that will eventually result in restarting an <see cref="AzureServiceBusMessagePump"/> instance.
    /// </summary>
    [Obsolete("To auto-restart Azure Service Bus message pumps upon rotated credentials, please use the 'Arcus.BackgroundJobs.KeyVault' package instead")]
    public class ReAuthenticateOnRotatedCredentialsMessageHandler : IAzureServiceBusMessageHandler<CloudEvent>
    {
        private readonly string _targetConnectionStringKey;
        private readonly AzureServiceBusMessagePump _messagePump;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReAuthenticateOnRotatedCredentialsMessageHandler"/> class.
        /// </summary>
        /// <param name="targetConnectionStringKey">The secret key where the connection string credentials are located for the target message pump that needs to be auto-restarted.</param>
        /// <param name="messagePump">The message pump instance to restart when the message handler process an <see cref="SecretNewVersionCreated"/> event.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messagePump"/> is <c>null</c>.</exception>
        public ReAuthenticateOnRotatedCredentialsMessageHandler(
            string targetConnectionStringKey, 
            AzureServiceBusMessagePump messagePump)
            : this(targetConnectionStringKey, messagePump, NullLogger<ReAuthenticateOnRotatedCredentialsMessageHandler>.Instance)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ReAuthenticateOnRotatedCredentialsMessageHandler"/> class.
        /// </summary>
        /// <param name="targetConnectionStringKey">The secret key where the connection string credentials are located for the target message pump that needs to be auto-restarted.</param>
        /// <param name="messagePump">The message pump instance to restart when the message handler process an <see cref="SecretNewVersionCreated"/> event.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the processing of the event.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messagePump"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public ReAuthenticateOnRotatedCredentialsMessageHandler(
            string targetConnectionStringKey, 
            AzureServiceBusMessagePump messagePump, 
            ILogger<ReAuthenticateOnRotatedCredentialsMessageHandler> logger)
        {
            Guard.NotNullOrWhitespace(targetConnectionStringKey, nameof(targetConnectionStringKey), "Requires a non-blank secret key that points to the credentials that holds the connection string of the target message pump");
            Guard.NotNull(messagePump, nameof(messagePump), $"Requires an message pump instance to restart when the message handler process an {nameof(SecretNewVersionCreated)} event");
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write diagnostic trace messages during the processing of the event");

            _targetConnectionStringKey = targetConnectionStringKey;
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
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="CloudException">Thrown when the <paramref name="message"/> doesn't represent an <see cref="SecretNewVersionCreated"/> event.</exception>
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
                _logger.LogWarning("Azure Key Vault job cannot map Event Grid event to CloudEvent because the event data isn't recognized as a 'SecretNewVersionCreated' schema");
            }
            else
            {
                if (_targetConnectionStringKey == secretNewVersionCreated.ObjectName)
                {
                    _logger.LogTrace("Received Azure Key vault 'Secret New Version Created' event, restarting target message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}'", _messagePump.JobId, _messagePump.EntityPath, _messagePump.Namespace);
                    await _messagePump.RestartAsync();
                    _logger.LogEvent("Message pump restarted", new Dictionary<string, object>
                    {
                        ["JobId"] = _messagePump.JobId,
                        ["EntityPath"] = _messagePump.EntityPath,
                        ["Namespace"] = _messagePump.Namespace
                    });
                }
                else
                {
                    _logger.LogTrace("Received Azure Key Vault 'Secret New Version Created' event for another secret, ignoring.");
                }
            }
        }
    }
}
