using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Provides an abstract base class for implementing a message pump that processes messages from an Azure Service Bus entity.
    /// </summary>
    /// <remarks>
    ///     This class extends <see cref="BackgroundService"/> to provide a framework for processing messages from Azure Service Bus entities,
    ///     such as queues or topics. It includes support for message routing, logging, and graceful shutdown handling.
    ///     Derived classes must implement the <see cref="StartProcessingMessagesAsync(CancellationToken)"/> method to define the message processing logic.
    /// </remarks>
    internal abstract class ServiceBusMessagePump : BackgroundService
    {
        private readonly ServiceBusMessageRouter _messageRouter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePump"/> class.
        /// </summary>
        protected ServiceBusMessagePump(
            ServiceBusEntityType entityType,
            string entityName,
            string subscriptionName,
            IServiceProvider serviceProvider,
            ServiceBusMessagePumpOptions options,
            ILogger logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(subscriptionName) && entityType is ServiceBusEntityType.Topic)
            {
                throw new ArgumentException(
                    $"The {nameof(subscriptionName)} must be specified when the {nameof(entityType)} is '{ServiceBusEntityType.Topic}', but was not provided.",
                    nameof(subscriptionName));
            }

            EntityType = entityType;
            EntityName = entityName;
            SubscriptionName = subscriptionName;
            Options = options;
            ServiceProvider = serviceProvider;
            Logger = logger ?? NullLogger.Instance;

            _messageRouter = new ServiceBusMessageRouter(serviceProvider, options.Routing, serviceProvider.GetService<ILogger<ServiceBusMessageRouter>>());
        }

        /// <summary>
        /// Gets the unique identifier of the job that this message pump is associated with.
        /// </summary>
        internal string JobId => Options.JobId;

        /// <summary>
        /// Gets a boolean flag that indicates whether the message pump is currently shutting down.
        /// </summary>
        protected bool IsHostShuttingDown { get; private set; }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity that this message pump is processing messages for.
        /// </summary>
        protected ServiceBusEntityType EntityType { get; }

        /// <summary>
        /// Gets the name of the Azure Service Bus entity that this message pump is processing messages for.
        /// </summary>
        protected string EntityName { get; }

        /// <summary>
        /// Gets the name of the topic subscription for the Azure Service Bus entity that this message pump is processing messages for.
        /// </summary>
        /// <remarks>
        ///     Only available when the <see cref="EntityType"/> is <see cref="ServiceBusEntityType.Topic"/>.
        /// </remarks>
        protected string SubscriptionName { get; }

        /// <summary>
        /// Gets the namespace of the Azure Service Bus entity that this message pump is processing messages for.
        /// </summary>
        protected abstract string Namespace { get; }

        /// <summary>
        /// Gets the options the user has configured for this message pump.
        /// </summary>
        protected ServiceBusMessagePumpOptions Options { get; }

        /// <summary>
        /// Gets the registered application services that can be used to resolve dependencies for message processing.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the logger that is used to log messages during the lifetime of this message pump.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// This method is called when the <see cref="IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long-running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="Task" /> that represents the long-running operations.</returns>
        /// <remarks>See <see href="https://docs.microsoft.com/dotnet/core/extensions/workers">Worker Services in .NET</see> for implementation guidelines.</remarks>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await StartProcessingMessagesAsync(stoppingToken);
                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is TaskCanceledException || exception is OperationCanceledException)
            {
#pragma warning disable CS0618 // Type or member is obsolete: the entity type will be moved down to this message pump in v3.0.
                Logger.LogDebug(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' was cancelled", EntityType, JobId, EntityName, Namespace);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occurred during processing of messages in the Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", EntityType, JobId, EntityName, Namespace);
            }
            finally
            {
                await StopProcessingMessagesAsync();
            }
        }

        private static async Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }

        /// <summary>
        /// Sets up the message pump to start processing messages from the Azure Service Bus entity.
        /// </summary>
        protected abstract Task StartProcessingMessagesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Routes the received message to the appropriate registered message handler.
        /// </summary>
        protected async Task<MessageProcessingResult> RouteMessageAsync(ServiceBusReceivedMessage message, AzureServiceBusMessageContext messageContext, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(messageContext);

            if (IsHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure Service Bus {EntityType} message pump '{JobId}' is shutting down", message.MessageId, EntityType, JobId);
                await messageContext.AbandonMessageAsync(new Dictionary<string, object>(), cancellationToken);
                return MessageProcessingResult.Failure(message.MessageId, MessageProcessingError.ProcessingInterrupted, "Cannot process received message as the message pump is shutting down");
            }

#pragma warning disable CS0618 // Type or member is obsolete
            using MessageCorrelationResult correlationResult = DetermineMessageCorrelation(message);

            MessageProcessingResult routingResult = await _messageRouter.RouteMessageAsync(message, messageContext, correlationResult.CorrelationInfo, cancellationToken);
            return routingResult;
        }

        private MessageCorrelationResult DetermineMessageCorrelation(ServiceBusReceivedMessage message)
        {
            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();
            var client = ServiceProvider.GetRequiredService<TelemetryClient>();

            return MessageCorrelationResult.Create(client, transactionId, operationParentId);

        }

        /// <summary>
        /// Sets up the message pump to stop processing messages from the Azure Service Bus entity.
        /// </summary>
        protected virtual Task StopProcessingMessagesAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        /// <returns>A <see cref="Task" /> that represents the asynchronous Stop operation.</returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            IsHostShuttingDown = true;
            return base.StopAsync(cancellationToken);
        }
    }
}