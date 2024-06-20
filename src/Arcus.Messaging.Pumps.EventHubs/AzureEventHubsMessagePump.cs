using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.EventHubs.Configuration;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.EventHubs
{
    /// <summary>
    /// Represents a message pump for processing messages on an Azure EventHubs resource.
    /// </summary>
    /// <seealso cref="MessagePump"/>
    public class AzureEventHubsMessagePump : MessagePump, IRestartableMessagePump
    {
        private readonly AzureEventHubsMessagePumpConfig _eventHubsConfig;
        private readonly IAzureEventHubsMessageRouter _messageRouter;
        private readonly IDisposable _loggingScope;

        private EventProcessorClient _eventProcessor;
        private bool _isHostShuttingDown;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessagePump" /> class.
        /// </summary>
        /// <param name="eventHubsConfiguration">The configuration instance to setup the interaction with Azure EventHubs.</param>
        /// <param name="applicationConfiguration">The application configuration instance to retrieve additional information for the message pump.</param>
        /// <param name="serviceProvider">The application's service provider to retrieve registered services during the lifetime of the message pump, like registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/>'s and its dependencies.</param>
        /// <param name="messageRouter">The registered message router to route incoming Azure EventHubs event messages through user-registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/>'s.</param>
        /// <param name="logger">The logger instance to write diagnostic messages during the lifetime of the message pump.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="eventHubsConfiguration"/>, <paramref name="applicationConfiguration"/>,
        ///     <paramref name="serviceProvider"/>, <paramref name="messageRouter"/>, or the <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        internal AzureEventHubsMessagePump(
            AzureEventHubsMessagePumpConfig eventHubsConfiguration,
            IConfiguration applicationConfiguration,
            IServiceProvider serviceProvider,
            IAzureEventHubsMessageRouter messageRouter,
            ILogger<AzureEventHubsMessagePump> logger) 
            : base(applicationConfiguration, serviceProvider, logger)
        {
            Guard.NotNull(eventHubsConfiguration, nameof(eventHubsConfiguration), "Requires an Azure EventHubs configuration instance to setup the interaction with the Azure EventHubs when consuming event messages");
            Guard.NotNull(applicationConfiguration, nameof(applicationConfiguration), "Requires an application configuration instance to retrieve additional information for the message pump");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an application's service provider to retrieve registered services during the lifetime of the message pump, like Azure EventHubs message handlers and its dependencies");
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires an Azure EventHubs message router when consuming event messages from Azure EventHubs and routing them though Azure EventHubs message handlers");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic messages during the lifetime of the message pump");

            _eventHubsConfig = eventHubsConfiguration;
            _messageRouter = messageRouter;
            
            JobId = _eventHubsConfig.Options.JobId;
            _loggingScope = logger.BeginScope("Job: {JobId}", JobId);
        }

        private string EventHubName => _eventProcessor?.EventHubName;
        private string ConsumerGroup => _eventProcessor?.ConsumerGroup;
        private string Namespace => _eventProcessor?.FullyQualifiedNamespace;

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await StartProcessingMessagesAsync(stoppingToken);
                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is TaskCanceledException || exception is OperationCanceledException)
            {
                Logger.LogDebug("Azure EventHubs message pump '{JobId}' '{ConsumerGroup}/{EventHubsName}' in '{Namespace}' is cancelled", JobId, ConsumerGroup, EventHubName, Namespace);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occurred during processing of messages in the Azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}': {Message}", JobId, ConsumerGroup, EventHubName, Namespace, exception.Message); 
            }
            finally
            {
                await StopProcessingMessagesAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Programmatically restart the message pump.
        /// </summary>
        /// <param name="cancellationToken">The token to cancel the restart process.</param>
        public async Task RestartAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Restarting Azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}' ...", JobId, ConsumerGroup, EventHubName, Namespace);
            await StopProcessingMessagesAsync(cancellationToken);
            await StartProcessingMessagesAsync(cancellationToken);
            Logger.LogInformation("Azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}' restarted!", JobId, ConsumerGroup, EventHubName, Namespace);
        }

        /// <inheritdoc />
        public override async Task StartProcessingMessagesAsync(CancellationToken stoppingToken)
        {
            await base.StartProcessingMessagesAsync(stoppingToken);

            _eventProcessor = await _eventHubsConfig.CreateEventProcessorClientAsync();
            _eventProcessor.ProcessEventAsync += ProcessMessageAsync;
            _eventProcessor.ProcessErrorAsync += ProcessErrorAsync;
            
            Logger.LogTrace("Starting Azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}'", JobId, ConsumerGroup, EventHubName, Namespace);
            await _eventProcessor.StartProcessingAsync(stoppingToken);
            Logger.LogInformation("Azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}' started: {Time}", JobId, ConsumerGroup, EventHubName, Namespace, DateTimeOffset.UtcNow);
        }

        private async Task ProcessMessageAsync(ProcessEventArgs args)
        {
            EventData message = args.Data;
            if (message is null)
            {
                Logger.LogWarning("Received message on Azure EventHubs message pump '{JobId}' was null, skipping", JobId);
                return;
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure EventHubs message pump is shutting down",  args.Data.MessageId);
                return;
            }

            if (string.IsNullOrEmpty(message.CorrelationId))
            {
                Logger.LogTrace("No operation ID was found on the message '{MessageId}' during processing in the Azure EventHubs message pump '{JobId}'", message.MessageId, JobId);
            }

            AzureEventHubsMessageContext context = args.Data.GetMessageContext(_eventProcessor, JobId);
            using (MessageCorrelationResult result = DetermineMessageCorrelation(args.Data))
            {
                await _messageRouter.RouteMessageAsync(args.Data, context, result.CorrelationInfo, args.CancellationToken);
                await args.UpdateCheckpointAsync(args.CancellationToken);
            }
        }

        private MessageCorrelationResult DetermineMessageCorrelation(EventData message)
        {
            if (_eventHubsConfig.Options.Routing.Correlation.Format is MessageCorrelationFormat.W3C)
            {
                (string transactionId, string operationParentId) = message.Properties.GetTraceParent();
                var client = ServiceProvider.GetRequiredService<TelemetryClient>();
                return MessageCorrelationResult.Create(client, transactionId, operationParentId);
            }

            MessageCorrelationInfo correlation = message.GetCorrelationInfo(
                transactionIdPropertyName: _eventHubsConfig.Options.Routing.Correlation.TransactionIdPropertyName,
                operationParentIdPropertyName: _eventHubsConfig.Options.Routing.Correlation.OperationParentIdPropertyName);

            return MessageCorrelationResult.Create(correlation);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            if (args.Exception is null)
            {
                Logger.LogWarning("Thrown exception on Azure EventHubs message pump '{JobId}' was null, skipping", JobId);
            }
            else if (args.Exception is TaskCanceledException)
            {
                Logger.LogDebug("Azure EventHubs message pump '{JobId}' is cancelled", JobId);
            }
            else
            {
                Logger.LogCritical(args.Exception, "Unable to process message in Azure EventHubs message pump '{JobId}' from {ConsumerGroup}/{EventHubName} with client {ClientId}", JobId, ConsumerGroup, EventHubName, _eventProcessor.Identifier);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task StopProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            await base.StopProcessingMessagesAsync(cancellationToken);

            try
            {
                Logger.LogTrace("Stopping Azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}'",  JobId, ConsumerGroup, EventHubName , Namespace);
                await _eventProcessor.StopProcessingAsync(cancellationToken);
                _eventProcessor.ProcessEventAsync -= ProcessMessageAsync;
                _eventProcessor.ProcessErrorAsync -= ProcessErrorAsync;
                Logger.LogInformation("Azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}' stopped: {Time}",  JobId, ConsumerGroup, EventHubName , Namespace, DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Cannot correctly close the azure EventHubs message pump '{JobId}' on '{ConsumerGroup}/{EventHubsName}' in '{Namespace}': {Message}", JobId, ConsumerGroup, EventHubName , Namespace, exception.Message);
            }
        }

        private static async Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == false)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }

        /// <summary>
        ///     Triggered when the message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            _loggingScope?.Dispose();
            _isHostShuttingDown = true;
        }
    }
}
