using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
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
    ///     Message pump for processing messages on an Azure Service Bus entity while making use of Azure ServiceBus sessions.
    /// </summary>
    public class AzureServiceBusSessionMessagePump : MessagePump
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly IDisposable _loggingScope;

        private bool _isHostShuttingDown;
        private ServiceBusSessionProcessor _serviceBusSessionProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusSessionMessagePump"/> class.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="messageRouter"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal AzureServiceBusSessionMessagePump(
            AzureServiceBusSessionMessagePumpSettings settings,
            IServiceProvider serviceProvider,
            IAzureServiceBusMessageRouter messageRouter,
            ILogger<AzureServiceBusSessionMessagePump> logger)
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
        internal AzureServiceBusSessionMessagePumpSettings Settings { get; }

        /// <summary>
        /// Gets the user-configurable options of the message pump.
        /// </summary>
        public AzureServiceBusSessionMessagePumpOptions Options => Settings.Options;

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Gets the name of the topic subscription; combined from the <see cref="AzureServiceBusMessagePumpSettings.SubscriptionName"/> and the <see cref="MessagePump.JobId"/>.
        /// </summary>
        protected string SubscriptionName { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _serviceBusSessionProcessor = await Settings.CreateSessionProcessorAsync();

                Namespace = _serviceBusSessionProcessor.FullyQualifiedNamespace;
                _serviceBusSessionProcessor.ProcessMessageAsync += ProcessMessageAsync;
                _serviceBusSessionProcessor.ProcessErrorAsync += ProcessErrorAsync;

                await _serviceBusSessionProcessor.StartProcessingAsync(stoppingToken);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _isHostShuttingDown = true;

            if (_serviceBusSessionProcessor != null)
            {
                await _serviceBusSessionProcessor.StopProcessingAsync();
                await _serviceBusSessionProcessor.CloseAsync();
            }

            await base.StopAsync(cancellationToken);
            _loggingScope?.Dispose();
        }

        private async Task ProcessMessageAsync(ProcessSessionMessageEventArgs arg)
        {
            ServiceBusReceivedMessage message = arg?.Message;
            if (message is null)
            {
                Logger.LogWarning("Received message on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", Settings.ServiceBusEntity, JobId);
                return;
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure Service Bus {EntityType} message pump '{JobId}' is shutting down", message.MessageId, Settings.ServiceBusEntity, JobId);
                await arg.AbandonMessageAsync(message);
                return;
            }
            if (string.IsNullOrEmpty(message.CorrelationId))
            {
                Logger.LogTrace("No operation ID was found on the message '{MessageId}' during processing in the Azure Service Bus {EntityType} message pump '{JobId}'", message.MessageId, Settings.ServiceBusEntity, JobId);
            }

            using MessageCorrelationResult correlationResult = DetermineMessageCorrelation(message);

            var messageContext = AzureServiceBusMessageContext.Create(JobId, Settings.ServiceBusEntity, arg);

            var routingResult = await _messageRouter.RouteMessageAsync(arg.Message, messageContext, correlationResult.CorrelationInfo, arg.CancellationToken);

            if (routingResult.IsSuccessful && Settings.Options.AutoComplete)
            {
                try
                {
                    Logger.LogTrace("Auto-complete message '{MessageId}' (if needed) after processing in Azure Service Bus {EntityType} message pump '{JobId}'", message.MessageId, Settings.ServiceBusEntity, JobId);
                    await messageContext.CompleteMessageAsync(CancellationToken.None);
                }
                catch (ServiceBusException exception) when (
                    exception.Reason is ServiceBusFailureReason.MessageLockLost or ServiceBusFailureReason.SessionLockLost)
                //&& exception.Message.Contains("expired")
                //&& exception.Message.Contains("already")
                //&& exception.Message.Contains("removed"))
                {
#pragma warning disable CS0618 // Typ or member is obsolete: entity type will be moved to this message pump in v3.0.
                    Logger.LogTrace("Message '{MessageId}' on Azure Service Bus {EntityType} message pump '{JobId}' does not need to be auto-completed, because it was already settled", messageContext.MessageId, Settings.ServiceBusEntity, JobId);
                }
            }
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            if (arg.Exception is null)
            {
                Logger.LogWarning("Thrown exception on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", Settings.ServiceBusEntity, JobId);
                return Task.CompletedTask;
            }

            Logger.LogCritical(arg.Exception, "Message pump '{JobId}' was unable to process message: {Message}", JobId, arg.Exception.Message);
            return Task.CompletedTask;
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
