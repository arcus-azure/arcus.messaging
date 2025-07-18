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

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    public class AzureServiceBusMessagePump : MessagePump
    {
        private readonly string _entityName;
        private readonly AzureServiceBusMessagePumpOptions _options;
        private readonly Func<IServiceProvider, ServiceBusClient> _clientImplementationFactory;
        private readonly AzureServiceBusMessageRouter _messageRouter;
        private readonly IDisposable _loggingScope;

        private bool _isHostShuttingDown;
        private ServiceBusReceiver _messageReceiver;
        private CancellationTokenSource _receiveMessagesCancellation;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePump"/> class.
        /// </summary>
        internal AzureServiceBusMessagePump(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType entityType,
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            AzureServiceBusMessagePumpOptions options,
            IServiceProvider serviceProvider,
            ILogger<AzureServiceBusMessagePump> logger)
            : base(serviceProvider, logger)
        {
            JobId = options.JobId;
            EntityType = entityType;
            SubscriptionName = subscriptionName;
            _options = options;

            _entityName = entityName;
            _clientImplementationFactory = clientImplementationFactory;
            _messageRouter = new AzureServiceBusMessageRouter(ServiceProvider, options.Routing, ServiceProvider.GetService<ILogger<AzureServiceBusMessageRouter>>());
            _loggingScope = logger?.BeginScope("Job: {JobId}", JobId);
        }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity for which this message pump is configured.
        /// </summary>
        public ServiceBusEntityType EntityType { get; }

        /// <summary>
        /// Gets the user-configurable options of the message pump.
        /// </summary>
        [Obsolete("This property will be removed in a future release")]
        public AzureServiceBusMessagePumpOptions Options => _options;

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Gets the name of the topic subscription, when the message pump receives messages from an Azure Service Bus topic.
        /// </summary>
        protected string SubscriptionName { get; }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _messageReceiver = CreateMessageReceiver();
                Namespace = _messageReceiver.FullyQualifiedNamespace;

                await StartProcessingMessagesAsync(stoppingToken);
                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is TaskCanceledException or OperationCanceledException)
            {
                Logger.LogDebug(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' was cancelled", EntityType, JobId, _entityName, Namespace);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occurred during processing of messages in the Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", EntityType, JobId, _entityName, Namespace);
            }
            finally
            {
                await StopProcessingMessagesAsync(CancellationToken.None);
            }
        }

        private ServiceBusReceiver CreateMessageReceiver()
        {
            ServiceBusClient client = _clientImplementationFactory(ServiceProvider);

            return string.IsNullOrWhiteSpace(SubscriptionName)
                ? client.CreateReceiver(_entityName, new ServiceBusReceiverOptions { PrefetchCount = _options.PrefetchCount })
                : client.CreateReceiver(_entityName, SubscriptionName, new ServiceBusReceiverOptions { PrefetchCount = _options.PrefetchCount });
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

            Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' started", EntityType, JobId, _entityName, Namespace);

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

                    Logger.LogTrace(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' was cancelled", EntityType, JobId, _entityName, Namespace);
                    return;
                }
                catch (Exception exception)
                {
                    await ProcessErrorAsync(exception);
                }
            }
        }

        private async Task ProcessMultipleMessagesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages =
                await _messageReceiver.ReceiveMessagesAsync(_options.MaxConcurrentCalls, cancellationToken: _receiveMessagesCancellation.Token);

            await Task.WhenAll(messages.Select(msg => ProcessMessageAsync(msg, cancellationToken)));
        }

        private async Task<MessageProcessingResult> TryProcessProcessSingleMessageAsync()
        {
            if (_messageReceiver is null)
            {
                throw new InvalidOperationException(
                    $"Cannot try process a single message in the Azure Service Bus {_entityName} message pump '{JobId}' because there was not a message receiver set before this point, " +
                    $"this probably happens when the message pump is used in the wrong way or manually called, please let only the circuit breaker functionality call this functionality");
            }

            Logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' tries to process single message during half-open circuit...", EntityType, JobId, _entityName, Namespace);

            ServiceBusReceivedMessage message = null;
            while (message is null)
            {
                message = await _messageReceiver.ReceiveMessageAsync();
                if (message is null)
                {
                    Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' failed to receive a single message, trying again...", EntityType, JobId, _entityName, Namespace);
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
                Logger.LogError(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' failed to process single message during half-open circuit, retrying after circuit delay", EntityType, JobId, _entityName, Namespace);
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

            Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' closed : {Time}", EntityType, JobId, _entityName, Namespace, DateTimeOffset.UtcNow);

            if (_receiveMessagesCancellation != null)
            {
                await _receiveMessagesCancellation.CancelAsync();
            }

            await base.StopProcessingMessagesAsync(cancellationToken);

        }

        private async Task ProcessErrorAsync(Exception exception)
        {
            if (exception is null)
            {
                Logger.LogWarning("Thrown exception on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", EntityType, JobId);
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
                Logger.LogWarning("Received message on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", EntityType, JobId);
                return MessageProcessingResult.Failure("<unavailable>", MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message is was 'null'");
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure Service Bus {EntityType} message pump '{JobId}' is shutting down", message.MessageId, EntityType, JobId);
                await _messageReceiver.AbandonMessageAsync(message);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message pump is shutting down");
            }

            if (string.IsNullOrEmpty(message.CorrelationId))
            {
                Logger.LogTrace("No operation ID was found on the message '{MessageId}' during processing in the Azure Service Bus {EntityType} message pump '{JobId}'", message.MessageId, EntityType, JobId);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            using MessageCorrelationResult correlationResult = DetermineMessageCorrelation(message);
            var messageContext = AzureServiceBusMessageContext.Create(JobId, EntityType, _messageReceiver, message);

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
