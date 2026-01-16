using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a message pump that processes messages from an Azure Service Bus entity via a <see cref="ServiceBusReceiver"/>.
    /// </summary>
    internal class ServiceBusReceiverMessagePump : ServiceBusMessagePump
    {
        private readonly Func<IServiceProvider, ServiceBusClient> _clientImplementationFactory;
        private static readonly TimeSpan WaitBetweenSingleMessageReceiveInHalfOpenState = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusReceiverMessagePump"/> class.
        /// </summary>
        internal ServiceBusReceiverMessagePump(
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            ServiceBusEntityType entityType,
            string entityName,
            string subscriptionName,
            IServiceProvider serviceProvider,
            ServiceBusMessagePumpOptions options,
            ILogger logger)
            : base(entityType, entityName, subscriptionName, serviceProvider, options, logger)
        {
            _clientImplementationFactory = clientImplementationFactory;
        }

        /// <summary>
        /// Gets the boolean flag that indicates whether the message pump is started.
        /// </summary>
        internal bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the current state of the message pump within the circuit breaker context.
        /// </summary>
        internal MessagePumpCircuitState CircuitState { get; private set; } = MessagePumpCircuitState.Closed;

        /// <summary>
        /// Sets up the message pump to start processing messages from the Azure Service Bus entity.
        /// </summary>
        protected override async Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            if (IsStarted)
            {
                return;
            }

            IsStarted = true;
            CircuitState = MessagePumpCircuitState.Closed;

#pragma warning disable S1135 // TODO: is valid
            /* TODO: we can't support Azure Service Bus plug-ins yet because the new Azure SDK doesn't yet support this:
                   https://github.com/arcus-azure/arcus.messaging/issues/176 */
#pragma warning restore S1135

            await using ServiceBusReceiver messageReceiver = CreateMessageReceiver();
            Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' started", EntityType, JobId, EntityName);

            while (CircuitState.IsClosed && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMultipleMessagesAsync(messageReceiver, cancellationToken);

                    if (CircuitState.IsOpen)
                    {
                        await WaitMessageRecoveryPeriodAsync(cancellationToken);

                        MessageProcessingResult singleProcessingResult;
                        do
                        {
                            singleProcessingResult = await TryProcessProcessSingleMessageAsync(messageReceiver, cancellationToken);
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

                    Logger.LogTrace(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' was cancelled", EntityType, JobId, EntityName);
                    return;
                }
                catch (Exception exception)
                {
                    Logger.LogCritical(exception, "Azure Service Bus message pump '{JobId}' was unable to process message", JobId);
                }
            }
        }

        private ServiceBusReceiver CreateMessageReceiver()
        {
            ServiceBusClient client = _clientImplementationFactory(ServiceProvider);

            return string.IsNullOrWhiteSpace(SubscriptionName)
                ? client.CreateReceiver(EntityName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount })
                : client.CreateReceiver(EntityName, SubscriptionName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount });
        }

        private async Task ProcessMultipleMessagesAsync(ServiceBusReceiver messageReceiver, CancellationToken cancellationToken)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages =
                await messageReceiver.ReceiveMessagesAsync(Options.MaxMessagesPerBatch, cancellationToken: cancellationToken);

            await Task.WhenAll(messages.Select(msg => ProcessMessageAsync(messageReceiver, msg, cancellationToken)));
        }

        private async Task<MessageProcessingResult> TryProcessProcessSingleMessageAsync(ServiceBusReceiver messageReceiver, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' tries to process single message during half-open circuit...", EntityType, JobId, EntityName);

            ServiceBusReceivedMessage message = null;
            while (message is null)
            {
                message = await messageReceiver.ReceiveMessageAsync(cancellationToken: cancellationToken);
                if (message is null)
                {
                    Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' failed to receive a single message, trying again...", EntityType, JobId, EntityName);
                    await Task.Delay(WaitBetweenSingleMessageReceiveInHalfOpenState, cancellationToken);
                }
            }

            try
            {
                MessageProcessingResult isSuccessfullyProcessed = await ProcessMessageAsync(messageReceiver, message, cancellationToken);
                return isSuccessfullyProcessed;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' failed to process single message during half-open circuit, retrying after circuit delay", EntityType, JobId, EntityName);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Failed to process single message during half-open circuit due to an unexpected exception", exception);
            }
        }

        private async Task<MessageProcessingResult> ProcessMessageAsync(ServiceBusReceiver messageReceiver, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        {
            if (message is null)
            {
                Logger.LogWarning("Received message on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", EntityType, JobId);
                return MessageProcessingResult.Failure("<unavailable>", MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message is was 'null'");
            }

            var messageContext = ServiceBusMessageContext.Create(JobId, EntityType, messageReceiver, message);

            MessageProcessingResult routingResult = await RouteMessageAsync(message, messageContext, cancellationToken);
            return routingResult;
        }

        /// <summary>
        /// Sets up the message pump to stop processing messages from the Azure Service Bus entity.
        /// </summary>
        protected override Task StopProcessingMessagesAsync()
        {
            if (!IsStarted)
            {
                return Task.CompletedTask;
            }

            IsStarted = false;
            CircuitState = CircuitState.TransitionTo(CircuitBreakerState.Open);

            Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' closed : {Time}", EntityType, JobId, EntityName, DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }

        private async Task WaitMessageRecoveryPeriodAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to wait message recovery period of '{Recovery}' during '{State}' state", JobId, CircuitState.Options.MessageRecoveryPeriod.ToString("g"), CircuitState);
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
            Logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to wait message interval during recovery of '{Interval}' during the '{State}' state", JobId, CircuitState.Options.MessageIntervalDuringRecovery.ToString("g"), CircuitState);
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
            Logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to transition from a '{CurrentState}' an 'Open' state", JobId, CircuitState);

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
            Logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to transition back from '{CurrentState}' to a 'Closed' state, retrieving messages is resumed", JobId, CircuitState);

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
            return ServiceProvider.GetServices<CircuitBreakerEventHandler>()
                                  .Where(registration => registration.JobId == JobId)
                                  .Select(handler => handler.Handler)
                                  .ToArray();
        }
    }
}
