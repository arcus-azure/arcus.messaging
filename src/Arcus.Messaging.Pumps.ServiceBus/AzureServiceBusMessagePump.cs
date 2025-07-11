using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618 // Type or member is obsolete: lots of deprecated functionality will be removed in v3.0.

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    public class AzureServiceBusMessagePump : MessagePump
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly IDisposable _loggingScope;

        private bool _isHostShuttingDown;
        private ServiceBusReceiver _messageReceiver;
        private CancellationTokenSource _receiveMessagesCancellation;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePump"/> class.
        /// </summary>
        /// <param name="settings">Settings to configure the message pump</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="messageRouter">The router to route incoming Azure Service Bus messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.</param>
        /// <param name="logger">Logger to write telemetry to</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/>, <paramref name="settings"/>, <paramref name="serviceProvider"/>, <paramref name="messageRouter"/> is <c>null</c>.</exception>
        internal AzureServiceBusMessagePump(
            AzureServiceBusMessagePumpSettings settings,
            IServiceProvider serviceProvider,
            IAzureServiceBusMessageRouter messageRouter,
            ILogger<AzureServiceBusMessagePump> logger)
            : base(serviceProvider, logger)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            JobId = Settings.Options.JobId;
            SubscriptionName = Settings.SubscriptionName;

            _messageRouter = messageRouter ?? throw new ArgumentNullException(nameof(messageRouter));
            _loggingScope = logger?.BeginScope("Job: {JobId}", JobId);
        }

        /// <summary>
        ///     Gets the settings configuring the message pump.
        /// </summary>
        internal AzureServiceBusMessagePumpSettings Settings { get; }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity for which this message pump is configured.
        /// </summary>
        public ServiceBusEntityType EntityType => Settings.ServiceBusEntity;

        /// <summary>
        /// Gets the user-configurable options of the message pump.
        /// </summary>
        public AzureServiceBusMessagePumpOptions Options => Settings.Options;

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Gets the name of the topic subscription; combined from the <see cref="AzureServiceBusMessagePumpSettings.SubscriptionName"/> and the <see cref="MessagePump.JobId"/>.
        /// </summary>
        protected string SubscriptionName { get; }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _messageReceiver = await Settings.CreateMessageReceiverAsync();

                await StartProcessingMessagesAsync(stoppingToken);
                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is TaskCanceledException || exception is OperationCanceledException)
            {
#pragma warning disable CS0618 // Type or member is obsolete: the entity type will be moved down to this message pump in v3.0.
                Logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' was cancelled", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occurred during processing of messages in the Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace);
            }
            finally
            {
                await StopProcessingMessagesAsync(CancellationToken.None);
            }
        }

        private static readonly TimeSpan MessagePollingWaitTime = TimeSpan.FromMilliseconds(300);

        /// <inheritdoc />
        public override async Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            if (IsStarted)
            {
                return;
            }

            await base.StartProcessingMessagesAsync(cancellationToken);

            /* TODO: we can't support Azure Service Bus plug-ins yet because the new Azure SDK doesn't yet support this:
                   https://github.com/arcus-azure/arcus.messaging/issues/176 */

            Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' started", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace);

            Namespace = _messageReceiver.FullyQualifiedNamespace;

            _receiveMessagesCancellation = new CancellationTokenSource();
            while (CircuitState.IsClosed
                   && !_messageReceiver.IsClosed
                   && !_receiveMessagesCancellation.IsCancellationRequested)
            {
                try
                {
                    await ProcessMultipleMessagesAsync(cancellationToken);

                    if (CircuitState.IsOpen)
                    {
                        await WaitMessageRecoveryPeriodAsync(cancellationToken);

                        MessageProcessingResult singleProcessingResult;
                        do
                        {
                            singleProcessingResult = await TryProcessProcessSingleMessageAsync();
                            if (!singleProcessingResult.IsSuccessful)
                            {
                                await WaitMessageIntervalDuringRecoveryAsync(cancellationToken);
                            }

                        } while (!singleProcessingResult.IsSuccessful);

                        NotifyResumeRetrievingMessages();
                    }
                }
                catch (Exception exception) when (exception is TaskCanceledException or OperationCanceledException or ObjectDisposedException)
                {
                    IsStarted = false;

                    Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' was cancelled", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace);
                    return;
                }
                catch (Exception exception)
                {
                    await ProcessErrorAsync(exception, cancellationToken);
                }
            }
        }

        private async Task ProcessMultipleMessagesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages =
                await _messageReceiver.ReceiveMessagesAsync(Settings.Options.MaxConcurrentCalls, cancellationToken: _receiveMessagesCancellation.Token);

            await Task.WhenAll(messages.Select(msg => ProcessMessageAsync(msg, cancellationToken)));
        }

        private async Task<MessageProcessingResult> TryProcessProcessSingleMessageAsync()
        {
            if (_messageReceiver is null)
            {
                throw new InvalidOperationException(
                    $"Cannot try process a single message in the Azure Service Bus {Settings.EntityName} message pump '{JobId}' because there was not a message receiver set before this point, " +
                    $"this probably happens when the message pump is used in the wrong way or manually called, please let only the circuit breaker functionality call this functionality");
            }

            Logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' tries to process single message during half-open circuit...", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace);

            ServiceBusReceivedMessage message = null;
            while (message is null)
            {
                message = await _messageReceiver.ReceiveMessageAsync();
                if (message is null)
                {
                    Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' failed to receive a single message, trying again...", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace);
                    await Task.Delay(MessagePollingWaitTime);
                }
            }

            try
            {
                MessageProcessingResult isSuccessfullyProcessed = await ProcessMessageAsync(message, CancellationToken.None);
                return isSuccessfullyProcessed;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' failed to process single message during half-open circuit, retrying after circuit delay", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Failed to process single message during half-open circuit due to an unexpected exception", exception);
            }
        }

        /// <inheritdoc />
        public override async Task StopProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            if (!IsStarted)
            {
                return;
            }

            Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' closed : {Time}", Settings.ServiceBusEntity, JobId, Settings.EntityName, Namespace, DateTimeOffset.UtcNow);

            _receiveMessagesCancellation?.Cancel();
            await base.StopProcessingMessagesAsync(cancellationToken);

        }

        private async Task ProcessErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            if (exception is null)
            {
                Logger.LogWarning("Thrown exception on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", Settings.ServiceBusEntity, JobId);
                return;
            }

            await HandleReceiveExceptionAsync(exception);
        }

        /// <summary>
        ///     Triggered when the Azure Service Bus message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_messageReceiver != null)
            {
                await _messageReceiver.CloseAsync();
            }

            await base.StopAsync(cancellationToken);
            _isHostShuttingDown = true;
            _loggingScope?.Dispose();
            _receiveMessagesCancellation?.Dispose();
        }

        private async Task<MessageProcessingResult> ProcessMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        {
            if (message is null)
            {
                Logger.LogWarning("Received message on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", Settings.ServiceBusEntity, JobId);
                return MessageProcessingResult.Failure("<unavailable>", MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message is was 'null'");
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure Service Bus {EntityType} message pump '{JobId}' is shutting down", message.MessageId, Settings.ServiceBusEntity, JobId);
                await _messageReceiver.AbandonMessageAsync(message);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message pump is shutting down");
            }

            if (string.IsNullOrEmpty(message.CorrelationId))
            {
                Logger.LogTrace("No operation ID was found on the message '{MessageId}' during processing in the Azure Service Bus {EntityType} message pump '{JobId}'", message.MessageId, Settings.ServiceBusEntity, JobId);
            }

            using MessageCorrelationResult correlationResult = DetermineMessageCorrelation(message);
            var messageContext = AzureServiceBusMessageContext.Create(JobId, Settings.ServiceBusEntity, _messageReceiver, message);

            MessageProcessingResult routingResult = await _messageRouter.RouteMessageAsync(message, messageContext, correlationResult.CorrelationInfo, cancellationToken);
            return routingResult;
        }

        private MessageCorrelationResult DetermineMessageCorrelation(ServiceBusReceivedMessage message)
        {
            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();
            var client = ServiceProvider.GetRequiredService<TelemetryClient>();

            return MessageCorrelationResult.Create(client, transactionId, operationParentId);

        }

        private static async Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
    }
}
