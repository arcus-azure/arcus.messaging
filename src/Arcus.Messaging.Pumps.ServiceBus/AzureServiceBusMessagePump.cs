using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    public class AzureServiceBusMessagePump : BackgroundService
    {
        private readonly string _entityName, _subscriptionName;
        private readonly Func<IServiceProvider, ServiceBusClient> _clientImplementationFactory;
        private readonly AzureServiceBusMessageRouter _messageRouter;
        private readonly IDisposable _loggingScope;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

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
        {
            JobId = options.JobId;
            EntityType = entityType;
            Options = options;

            _entityName = entityName;
            _subscriptionName = subscriptionName;
            _clientImplementationFactory = clientImplementationFactory;
            _serviceProvider = serviceProvider;
            _messageRouter = new AzureServiceBusMessageRouter(serviceProvider, options.Routing, serviceProvider.GetService<ILogger<AzureServiceBusMessageRouter>>());
            _logger = logger;
            _loggingScope = logger?.BeginScope("Job: {JobId}", JobId);
        }

        /// <summary>
        /// Gets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Gets the boolean flag that indicates whether the message pump is started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the current state of the message pump within the circuit breaker context.
        /// </summary>
        public MessagePumpCircuitState CircuitState { get; private set; } = MessagePumpCircuitState.Closed;

        /// <summary>
        /// Gets the type of the Azure Service Bus entity for which this message pump is configured.
        /// </summary>
        public ServiceBusEntityType EntityType { get; }

        /// <summary>
        /// Gets the user-configurable options of the message pump.
        /// </summary>
        public AzureServiceBusMessagePumpOptions Options { get; }

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        public string Namespace { get; private set; }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _messageReceiver = CreateMessageReceiver();

                await StartProcessingMessagesAsync(stoppingToken);
                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is TaskCanceledException or OperationCanceledException)
            {
                _logger.LogDebug(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' was cancelled", EntityType, JobId, _entityName, Namespace);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Unexpected failure occurred during processing of messages in the Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", EntityType, JobId, _entityName, Namespace);
            }
            finally
            {
                await StopProcessingMessagesAsync();
            }
        }

        private ServiceBusReceiver CreateMessageReceiver()
        {
            ServiceBusClient client = _clientImplementationFactory(_serviceProvider);

            return string.IsNullOrWhiteSpace(_subscriptionName)
                ? client.CreateReceiver(_entityName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount })
                : client.CreateReceiver(_entityName, _subscriptionName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount });
        }

        private static readonly TimeSpan MessagePollingWaitTime = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// Starts the message pump to start processing messages from the Azure Service Bus entity.
        /// </summary>
        public async Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            if (IsStarted)
            {
                return;
            }

            IsStarted = true;
            CircuitState = MessagePumpCircuitState.Closed;

            /* TODO: we can't support Azure Service Bus plug-ins yet because the new Azure SDK doesn't yet support this:
                   https://github.com/arcus-azure/arcus.messaging/issues/176 */

            _logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' started", EntityType, JobId, _entityName, Namespace);

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

                    _logger.LogTrace(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' was cancelled", EntityType, JobId, _entityName, Namespace);
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Azure Service Bus message pump '{JobId}' was unable to process message", JobId);
                }
            }
        }

        private async Task ProcessMultipleMessagesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages =
                await _messageReceiver.ReceiveMessagesAsync(Options.MaxConcurrentCalls, cancellationToken: _receiveMessagesCancellation.Token);

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

            _logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' tries to process single message during half-open circuit...", EntityType, JobId, _entityName, Namespace);

            ServiceBusReceivedMessage message = null;
            while (message is null)
            {
                message = await _messageReceiver.ReceiveMessageAsync();
                if (message is null)
                {
                    _logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' failed to receive a single message, trying again...", EntityType, JobId, _entityName, Namespace);
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
                _logger.LogError(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' failed to process single message during half-open circuit, retrying after circuit delay", EntityType, JobId, _entityName, Namespace);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Failed to process single message during half-open circuit due to an unexpected exception", exception);
            }
        }

        /// <summary>
        /// Stops the message pump to stop processing messages from the Azure Service Bus entity.
        /// </summary>
        public async Task StopProcessingMessagesAsync()
        {
            if (!IsStarted)
            {
                return;
            }

            IsStarted = false;
            CircuitState = CircuitState.TransitionTo(CircuitBreakerState.Open);

            _logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' closed : {Time}", EntityType, JobId, _entityName, Namespace, DateTimeOffset.UtcNow);

            if (_receiveMessagesCancellation != null)
            {
                await _receiveMessagesCancellation.CancelAsync();
            }
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
                _logger.LogWarning("Received message on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", EntityType, JobId);
                return MessageProcessingResult.Failure("<unavailable>", MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message is was 'null'");
            }

            if (_isHostShuttingDown)
            {
                _logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure Service Bus {EntityType} message pump '{JobId}' is shutting down", message.MessageId, EntityType, JobId);
                await _messageReceiver.AbandonMessageAsync(message);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message pump is shutting down");
            }

            if (string.IsNullOrEmpty(message.CorrelationId))
            {
                _logger.LogTrace("No operation ID was found on the message '{MessageId}' during processing in the Azure Service Bus {EntityType} message pump '{JobId}'", message.MessageId, EntityType, JobId);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            using MessageCorrelationResult correlationResult = DetermineMessageCorrelation(message);
            var messageContext = ServiceBusMessageContext.Create(JobId, EntityType, _messageReceiver, message);

            MessageProcessingResult routingResult = await _messageRouter.RouteMessageAsync(_messageReceiver, message, messageContext, correlationResult.CorrelationInfo, cancellationToken);
            return routingResult;
        }

        private MessageCorrelationResult DetermineMessageCorrelation(ServiceBusReceivedMessage message)
        {
            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();
            var client = _serviceProvider.GetRequiredService<TelemetryClient>();

            return MessageCorrelationResult.Create(client, transactionId, operationParentId);

        }

        private static async Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }

        private async Task WaitMessageRecoveryPeriodAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to wait message recovery period of '{Recovery}' during '{State}' state", JobId, CircuitState.Options.MessageRecoveryPeriod.ToString("g"), CircuitState);
            await Task.Delay(CircuitState.Options.MessageRecoveryPeriod, cancellationToken);

            if (!CircuitState.IsHalfOpen)
            {
                MessagePumpCircuitState
                    oldState = CircuitState,
                    newState = CircuitState.TransitionTo(CircuitBreakerState.HalfOpen);

                CircuitState = newState;
                NotifyCircuitBreakerStateChangedSubscribers(oldState, newState);
            }
        }

        private async Task WaitMessageIntervalDuringRecoveryAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to wait message interval during recovery of '{Interval}' during the '{State}' state", JobId, CircuitState.Options.MessageIntervalDuringRecovery.ToString("g"), CircuitState);
            await Task.Delay(CircuitState.Options.MessageIntervalDuringRecovery, cancellationToken);

            if (!CircuitState.IsHalfOpen)
            {
                MessagePumpCircuitState
                    oldState = CircuitState,
                    newState = CircuitState.TransitionTo(CircuitBreakerState.HalfOpen);

                CircuitState = newState;
                NotifyCircuitBreakerStateChangedSubscribers(oldState, newState);
            }
        }

        /// <summary>
        /// Notifies the message pump about the new state which pauses message retrieval.
        /// </summary>
        /// <param name="options">The additional accompanied options that goes with the new state.</param>
        internal void NotifyPauseReceiveMessages(MessagePumpCircuitBreakerOptions options)
        {
            _logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to transition from a '{CurrentState}' an 'Open' state", JobId, CircuitState);

            MessagePumpCircuitState
                oldState = CircuitState,
                newState = CircuitState.TransitionTo(CircuitBreakerState.Open, options);

            CircuitState = newState;
            NotifyCircuitBreakerStateChangedSubscribers(oldState, newState);
        }

        /// <summary>
        /// Notifies the message pump about the new state which resumes message retrieval.
        /// </summary>
        internal void NotifyResumeRetrievingMessages()
        {
            _logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to transition back from '{CurrentState}' to a 'Closed' state, retrieving messages is resumed", JobId, CircuitState);

            MessagePumpCircuitState
                oldState = CircuitState,
                newState = MessagePumpCircuitState.Closed;

            CircuitState = newState;
            NotifyCircuitBreakerStateChangedSubscribers(oldState, newState);
        }

        private void NotifyCircuitBreakerStateChangedSubscribers(MessagePumpCircuitState oldState, MessagePumpCircuitState newState)
        {
            ICircuitBreakerEventHandler[] eventHandlers = GetEventHandlersForPump();
            foreach (var handler in eventHandlers)
            {
                Task.Run(() => handler.OnTransition(new MessagePumpCircuitStateChangedEventArgs(JobId, oldState, newState)));
            }
        }

        private ICircuitBreakerEventHandler[] GetEventHandlersForPump()
        {
            return _serviceProvider.GetServices<CircuitBreakerEventHandler>()
                                  .Where(registration => registration.JobId == JobId)
                                  .Select(handler => handler.Handler)
                                  .ToArray();
        }
    }
}
