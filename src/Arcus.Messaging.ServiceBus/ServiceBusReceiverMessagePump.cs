using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.ServiceBus;
using Arcus.Messaging.ServiceBus.Resiliency;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a message pump that processes messages from an Azure Service Bus entity via a <see cref="ServiceBusReceiver"/>.
    /// </summary>
    internal class ServiceBusReceiverMessagePump : ServiceBusMessagePump
    {
        private readonly Func<IServiceProvider, ServiceBusClient> _clientImplementationFactory;

        private readonly CircuitBreakerStateMachine _circuitBreakerStateMachine;

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
            _circuitBreakerStateMachine = new CircuitBreakerStateMachine(JobId, serviceProvider, logger);
        }

        private volatile bool _isStarted;

        /// <summary>
        /// Gets the boolean flag that indicates whether the message pump is started.
        /// </summary>
        internal bool IsStarted { get => _isStarted; private set => _isStarted = value; }

        /// <summary>
        /// Gets the current state of the message pump within the circuit breaker context.
        /// </summary>
        internal MessagePumpCircuitState CircuitState => _circuitBreakerStateMachine.Current;

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
            _circuitBreakerStateMachine.MarkPumpStarted();

#pragma warning disable S1135 // TODO: is valid
            /* TODO: we can't support Azure Service Bus plug-ins yet because the new Azure SDK doesn't yet support this:
                   https://github.com/arcus-azure/arcus.messaging/issues/176 */
#pragma warning restore S1135

            ServiceBusReceiver messageReceiver = CreateMessageReceiver();
            Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' started", EntityType, JobId, EntityName);

            var messageProcessingTasks = new List<Task>();
            try
            {
                while (CircuitState.IsClosed && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await RemoveCompletedMessageProcessingTasks(messageProcessingTasks);

                        while (messageProcessingTasks.Count >= Options.MaxMessagesPerBatch &&
                               CircuitState.IsClosed)
                        {
                            // As long as the circuit is closed, we can wait for any of the message processing tasks
                            // to complete.  As soon as one (ore more) messages are processed, we can receive new
                            // messages to fill up the empty slots.
                            await Task.WhenAny(messageProcessingTasks).WaitAsync(cancellationToken);
                            await RemoveCompletedMessageProcessingTasks(messageProcessingTasks);
                        }

                        IReadOnlyList<ServiceBusReceivedMessage> messages = Array.Empty<ServiceBusReceivedMessage>();
                        if (CircuitState.IsClosed)
                        {
                            int freeSlots = Options.MaxMessagesPerBatch - messageProcessingTasks.Count;
                            messages = await messageReceiver.ReceiveMessagesAsync(freeSlots, TimeSpan.FromSeconds(1), cancellationToken);
                        }

                        // Re-check: a handler may have opened the circuit while we were waiting or receiving.
                        if (!CircuitState.IsClosed)
                        {
                            // Abandon all messages that were freshly received while the circuit was open, so they can be redelivered later.
                            await AbandonReceivedMessagesAsync(messageReceiver, messages, cancellationToken);

                            await Task.WhenAll(messageProcessingTasks);
                            messageProcessingTasks.Clear();

                            await RecoverFromOpenCircuitAsync(messageReceiver, cancellationToken);
                            continue;
                        }

                        foreach (var message in messages)
                        {
                            messageProcessingTasks.Add(ProcessMessageAsync(messageReceiver, message, cancellationToken));
                        }
                    }
                    catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
                    {
                        Logger.LogTrace(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' was cancelled", EntityType, JobId, EntityName);
                        break;
                    }
                    catch (Exception exception)
                    {
                        Logger.LogCritical(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' ran into an unexpected error.", EntityType, JobId, EntityName);
                    }
                }
            }
            finally
            {
                IsStarted = false;
                _circuitBreakerStateMachine.MarkPumpStopped();

                await Task.WhenAll(messageProcessingTasks);
                await messageReceiver.DisposeAsync();
            }
        }

        private async Task RemoveCompletedMessageProcessingTasks(List<Task> messageProcessingTasks)
        {
            List<Task> completed = null;

            for (int i = messageProcessingTasks.Count - 1; i >= 0; i--)
            {
                Task task = messageProcessingTasks[i];
                if (task.IsCompleted)
                {
                    completed ??= new List<Task>();
                    completed.Add(task);
                    messageProcessingTasks.RemoveAt(i);
                }
            }

            if (completed is not null)
            {
                Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' is removing {Count} completed message processing tasks", EntityType, JobId, EntityName, completed.Count);
                await Task.WhenAll(completed);
            }
        }

        private async Task AbandonReceivedMessagesAsync(ServiceBusReceiver messageReceiver, IReadOnlyList<ServiceBusReceivedMessage> messages, CancellationToken cancellationToken)
        {
            if (messages.Count == 0)
            {
                return;
            }

            Logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' abandons {Count} received message(s) because the circuit breaker opened during receive", EntityType, JobId, EntityName, messages.Count);

            foreach (ServiceBusReceivedMessage message in messages)
            {
                try
                {
                    await messageReceiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Logger.LogWarning(exception, "Azure Service Bus {EntityType} message pump '{JobId}' failed to abandon message '{MessageId}' after the circuit opened; it will be redelivered after lock expiry", EntityType, JobId, message.MessageId);
                }
            }
        }

        /// <summary>
        /// Entered only while the circuit is Open: waits for the dependency to settle, transitions to HalfOpen,
        /// and probes with single messages until one succeeds, after which the circuit is Closed again.
        /// </summary>
        private async Task RecoverFromOpenCircuitAsync(ServiceBusReceiver messageReceiver, CancellationToken cancellationToken)
        {
            // Wait for the configured recovery period before transitioning into a half-open state
            // where we're probing with a single message to check if we can close the circuitbreaker again.
            await WaitMessageRecoveryPeriodAsync(cancellationToken);

            MessageProcessingResult probe;
            do
            {
                probe = await TryProcessProcessSingleMessageAsync(messageReceiver, cancellationToken);
                if (!probe.IsSuccessful)
                {
                    // The message could not be processed succesfully so we stay in half-open state.
                    await WaitMessageIntervalDuringRecoveryAsync(cancellationToken);
                }
            }
            while (!probe.IsSuccessful);

            _circuitBreakerStateMachine.TryClose();
        }

        private ServiceBusReceiver CreateMessageReceiver()
        {
            ServiceBusClient client = _clientImplementationFactory(ServiceProvider);

            return string.IsNullOrWhiteSpace(SubscriptionName)
                ? client.CreateReceiver(EntityName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount })
                : client.CreateReceiver(EntityName, SubscriptionName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount });
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

            MessageProcessingResult isSuccessfullyProcessed = await ProcessMessageAsync(messageReceiver, message, cancellationToken);
            return isSuccessfullyProcessed;
        }

        private async Task<MessageProcessingResult> ProcessMessageAsync(ServiceBusReceiver messageReceiver, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        {
            try
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogTrace("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' is shutting down - Processing message '{MessageId}' cancelled.", EntityType, JobId, EntityName, message?.MessageId);
                return MessageProcessingResult.Failure(message?.MessageId ?? "<unavailable>", MessageProcessingError.ProcessingInterrupted, "Message processing cancelled during shutdown");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' failed to process message '{MessageId}'", EntityType, JobId, EntityName, message?.MessageId);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Failed to process message due to an unexpected exception", exception);
            }
        }

        /// <summary>
        /// Sets up the message pump to stop processing messages from the Azure Service Bus entity.
        /// </summary>
        protected override Task StopProcessingMessagesAsync()
        {
            return Task.CompletedTask;
        }

        private async Task WaitMessageRecoveryPeriodAsync(CancellationToken cancellationToken)
        {
            MessagePumpCircuitState state = CircuitState;

            Logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to wait message recovery period of '{Recovery}' during '{State}' state", JobId, CircuitState.Options.MessageRecoveryPeriod.ToString("g"), state);
            await Task.Delay(state.Options.MessageRecoveryPeriod, cancellationToken);
            _circuitBreakerStateMachine.TryHalfOpen();
        }

        private async Task WaitMessageIntervalDuringRecoveryAsync(CancellationToken cancellationToken)
        {
            MessagePumpCircuitState state = CircuitState;

            Logger.LogDebug("Circuit breaker caused Azure Service Bus message pump '{JobId}' to wait message interval during recovery of '{Interval}' during the '{State}' state", JobId, state.Options.MessageIntervalDuringRecovery.ToString("g"), state);
            await Task.Delay(state.Options.MessageIntervalDuringRecovery, cancellationToken);
            _circuitBreakerStateMachine.TryHalfOpen();
        }

        /// <summary>
        /// Notifies the message pump about the new state which pauses message retrieval.
        /// </summary>
        /// <param name="options">The additional accompanied options that goes with the new state.</param>
        /// <returns><c>true</c> if the circuit breaker state was successfully changed to open; otherwise, <c>false</c>.</returns>
        internal bool NotifyPauseReceiveMessages(MessagePumpCircuitBreakerOptions options)
        {
            return _circuitBreakerStateMachine.TryOpen(options);
        }
    }
}