using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    public class AzureServiceBusMessagePump : MessagePump, IRestartableMessagePump
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly IDisposable _loggingScope;

        private bool _isHostShuttingDown;
        private ServiceBusProcessor _messageProcessor;
        private ServiceBusReceiver _messageReceiver;
        private int _unauthorizedExceptionCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePump"/> class.
        /// </summary>
        /// <param name="settings">Settings to configure the message pump</param>
        /// <param name="applicationConfiguration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="messageRouter">The router to route incoming Azure Service Bus messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.</param>
        /// <param name="logger">Logger to write telemetry to</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/>, <paramref name="settings"/>, <paramref name="serviceProvider"/>, <paramref name="messageRouter"/> is <c>null</c>.</exception>
        public AzureServiceBusMessagePump(
            AzureServiceBusMessagePumpSettings settings,
            IConfiguration applicationConfiguration, 
            IServiceProvider serviceProvider, 
            IAzureServiceBusMessageRouter messageRouter,
            ILogger<AzureServiceBusMessagePump> logger)
            : base(applicationConfiguration, serviceProvider, logger)
        {
            Guard.NotNull(settings, nameof(settings), "Requires a set of settings to correctly configure the message pump");
            Guard.NotNull(applicationConfiguration, nameof(applicationConfiguration), "Requires a configuration instance to retrieve application-specific information");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to retrieve the registered message handlers");
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires a message router to route incoming Azure Service Bus messages through registered message handlers");
            
            Settings = settings;
            JobId = Settings.Options.JobId;
            SubscriptionName = Settings.SubscriptionName;

            _messageRouter = messageRouter;
            _loggingScope = logger.BeginScope("Job: {JobId}", JobId);
        }

        /// <summary>
        ///     Gets the settings configuring the message pump.
        /// </summary>
        public AzureServiceBusMessagePumpSettings Settings { get; }

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Gets the name of the topic subscription; combined from the <see cref="AzureServiceBusMessagePumpSettings.SubscriptionName"/> and the <see cref="MessagePump.JobId"/>.
        /// </summary>
        protected string SubscriptionName { get; }

        /// <summary>
        /// Reconfigure the Azure Service Bus options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        public void ReconfigureOptions(Action<AzureServiceBusMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus options");

            reconfigure(Settings.Options);
        }

        /// <summary>
        /// Reconfigure the Azure Service Bus Queue options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus Queue options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the message pump is not configured for Queues.</exception>
        public void ReconfigureQueueOptions(Action<IAzureServiceBusQueueMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus Queue options");
            Guard.For<NotSupportedException>(
                () => Settings.ServiceBusEntity is ServiceBusEntityType.Topic, 
                "Requires the message pump to be configured for Azure Service Bus Queue to reconfigure these options, use the Topic overload instead");

            reconfigure(Settings.Options);
        }

        /// <summary>
        /// Reconfigure the Azure Service Bus Topic options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus Topic options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the message pump is not configured for Topics.</exception>
        public void ReconfigureTopicOptions(Action<IAzureServiceBusTopicMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus Topics options");
            Guard.For<NotSupportedException>(
                () => Settings.ServiceBusEntity is ServiceBusEntityType.Queue,
                "Requires a message pump to be configured for Azure Service Bus Topic to reconfigure these options, use the Queue overload instead");

            reconfigure(Settings.Options);
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Settings.ServiceBusEntity == ServiceBusEntityType.Topic
                && Settings.Options.TopicSubscription.HasValue
                && Settings.Options.TopicSubscription.Value.HasFlag(TopicSubscription.CreateOnStart))
            {
                await CreateTopicSubscriptionAsync(cancellationToken);
            }

            await base.StartAsync(cancellationToken);
        }

        private async Task CreateTopicSubscriptionAsync(CancellationToken cancellationToken)
        {
            ServiceBusAdministrationClient serviceBusClient = await Settings.GetServiceBusAdminClientAsync();
            string entityPath = await Settings.GetEntityPathAsync();
            
            try
            {
                bool subscriptionExists = await serviceBusClient.SubscriptionExistsAsync(entityPath, SubscriptionName, cancellationToken);
                if (subscriptionExists)
                {
                    Logger.LogTrace("Topic subscription with name '{SubscriptionName}' already exists on Service Bus resource", SubscriptionName);
                }
                else
                {
                    Logger.LogTrace("Creating subscription '{SubscriptionName}' on topic '{TopicPath}'...", SubscriptionName, entityPath);
                    
                    var subscriptionDescription = new CreateSubscriptionOptions(entityPath, SubscriptionName)
                    {
                        UserMetadata = $"Subscription created by Arcus job: '{JobId}' to process Service Bus messages."
                    };
                    var ruleDescription = new CreateRuleOptions("Accept-All", new TrueRuleFilter());
                    await serviceBusClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription, cancellationToken)
                                          .ConfigureAwait(continueOnCapturedContext: false);

                    Logger.LogTrace("Subscription '{SubscriptionName}' created on topic '{TopicPath}'", SubscriptionName, entityPath);
                }
            }
            catch (Exception exception) when (exception is not TaskCanceledException && exception is not OperationCanceledException)
            {
                Logger.LogWarning(exception, "Failed to create topic subscription with name '{SubscriptionName}' on Service Bus resource", SubscriptionName);
            }
        }

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
                Logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' was cancelled", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occurred during processing of messages in the Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
            }
            finally
            {
                await StopProcessingMessagesAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Try to process a single message after the circuit was broken, a.k.a entering the half-open state.
        /// </summary>
        /// <returns>
        ///     [true] when the related message handler can again process messages and the message pump can again start receive messages in full; [false] otherwise.
        /// </returns>
        public override async Task<bool> TryProcessProcessSingleMessageAsync(MessagePumpCircuitBreakerOptions options)
        {
            Logger.LogDebug("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' tries to process single message during half-open circuit...", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
            
            ServiceBusReceivedMessage message = null;
            while (message is null)
            {
                message = await _messageReceiver.ReceiveMessageAsync();
            }

            try
            {
                await ProcessMessageAsync(new ProcessMessageEventArgs(message, _messageReceiver, CancellationToken.None));
                return true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' failed to process single message during half-open circuit, retrying after circuit delay", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
                return false;
            }
        }

        /// <inheritdoc />
        public override async Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            if (_messageProcessor is null)
            {
                _messageProcessor = await Settings.CreateMessageProcessorAsync();
            }
            
            Namespace = _messageProcessor.FullyQualifiedNamespace;

            /* TODO: we can't support Azure Service Bus plug-ins yet because the new Azure SDK doesn't yet support this:
                     https://github.com/arcus-azure/arcus.messaging/issues/176 */

            RegisterClientInformation(JobId, _messageProcessor.EntityPath);
            
            Logger.LogTrace("Starting Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
            _messageProcessor.ProcessErrorAsync += ProcessErrorAsync;
            _messageProcessor.ProcessMessageAsync += ProcessMessageAsync;
            await _messageProcessor.StartProcessingAsync(cancellationToken);
            Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' started", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
        }

        /// <inheritdoc />
        public override async Task StopProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            if (_messageProcessor is null)
            {
                return;
            }

            try
            {
                Logger.LogTrace("Closing Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}'", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
                _messageProcessor.ProcessMessageAsync -= ProcessMessageAsync;
                _messageProcessor.ProcessErrorAsync -= ProcessErrorAsync;
                await _messageProcessor.StopProcessingAsync(cancellationToken);

                Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' closed : {Time}", Settings.ServiceBusEntity, JobId, EntityPath, Namespace, DateTimeOffset.UtcNow);
            }
            catch (Exception exception) when (exception is not TaskCanceledException && exception is not OperationCanceledException)
            {
                Logger.LogWarning(exception, "Cannot correctly close the Azure Service Bus message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}': {Message}",  JobId, EntityPath, Namespace, exception.Message);
            }
        }

        private async Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            if (args?.Exception is null)
            {
                Logger.LogWarning("Thrown exception on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", Settings.ServiceBusEntity, JobId);
                return;
            }
            
            try
            {
                await HandleReceiveExceptionAsync(args.Exception);
            }
            finally
            {
                if (args.Exception is UnauthorizedAccessException)
                {
                    if (Interlocked.Increment(ref _unauthorizedExceptionCount) >= Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart)
                    {
                        Logger.LogTrace("Unable to connect anymore to Azure Service Bus, trying to re-authenticate...");
                        await RestartAsync(args.CancellationToken);
                    }
                    else
                    {
                        Logger.LogWarning("Unable to connect anymore to Azure Service Bus ({CurrentCount}/{MaxCount})", 
                            _unauthorizedExceptionCount, Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart);
                    }
                }
            }
        }

        /// <summary>
        /// Restart core functionality of the message pump.
        /// </summary>
        public async Task RestartAsync()
        {
            Interlocked.Exchange(ref _unauthorizedExceptionCount, 0);

            Logger.LogTrace("Restarting Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' ...", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
            await StopProcessingMessagesAsync(CancellationToken.None);
            await StartProcessingMessagesAsync(CancellationToken.None);
            Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' restarted!", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
        }

        /// <summary>
        /// Restart core functionality of the message pump.
        /// </summary>
        public async Task RestartAsync(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _unauthorizedExceptionCount, 0);

            Logger.LogTrace("Restarting Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' ...", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
            await StopProcessingMessagesAsync(cancellationToken);
            await StartProcessingMessagesAsync(cancellationToken);
            Logger.LogInformation("Azure Service Bus {EntityType} message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' restarted!", Settings.ServiceBusEntity, JobId, EntityPath, Namespace);
        }

        /// <summary>
        ///     Triggered when the Azure Service Bus message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_messageProcessor != null)
            {
                await _messageProcessor.StopProcessingAsync();
                await _messageProcessor.CloseAsync();
            }

            if (_messageReceiver != null)
            {
                await _messageReceiver.CloseAsync();
            }

            if (Settings.ServiceBusEntity == ServiceBusEntityType.Topic
                && Settings.Options.TopicSubscription.HasValue
                && Settings.Options.TopicSubscription.Value.HasFlag(TopicSubscription.DeleteOnStop))
            {
                await DeleteTopicSubscriptionAsync(cancellationToken);
            }

            await base.StopAsync(cancellationToken);
            _isHostShuttingDown = true;
            _loggingScope?.Dispose();
        }

        private async Task DeleteTopicSubscriptionAsync(CancellationToken cancellationToken)
        {
            ServiceBusAdministrationClient serviceBusClient = await Settings.GetServiceBusAdminClientAsync();
            string entityPath = await Settings.GetEntityPathAsync();

            try
            {
                bool subscriptionExists =
                    await serviceBusClient.SubscriptionExistsAsync(entityPath, SubscriptionName, cancellationToken);
                if (subscriptionExists)
                {
                    Logger.LogTrace("Deleting subscription '{SubscriptionName}' on topic '{Path}'...", SubscriptionName, entityPath);
                    await serviceBusClient.DeleteSubscriptionAsync(entityPath, SubscriptionName, cancellationToken);
                    Logger.LogTrace("Subscription '{SubscriptionName}' deleted on topic '{Path}'", SubscriptionName, entityPath);
                }
                else
                {
                    Logger.LogTrace("Cannot delete topic subscription with name '{SubscriptionName}' because no subscription exists on Service Bus resource", SubscriptionName);
                }
            }
            catch (Exception exception) when (exception is not TaskCanceledException && exception is not OperationCanceledException)
            {
                Logger.LogWarning(exception, "Failed to delete topic subscription with name '{SubscriptionName}' on Service Bus resource", SubscriptionName);
            }
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            ServiceBusReceivedMessage message = args?.Message;
            if (message is null)
            {
                Logger.LogWarning("Received message on Azure Service Bus {EntityType} message pump '{JobId}' was null, skipping", Settings.ServiceBusEntity, JobId);
                return;
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the Azure Service Bus {EntityType} message pump '{JobId}' is shutting down",  message.MessageId, Settings.ServiceBusEntity, JobId);
                await args.AbandonMessageAsync(message);
                return;
            }

            if (string.IsNullOrEmpty(message.CorrelationId))
            {
                Logger.LogTrace("No operation ID was found on the message '{MessageId}' during processing in the Azure Service Bus {EntityType} message pump '{JobId}'", message.MessageId, Settings.ServiceBusEntity, JobId);
            }

            using (MessageCorrelationResult correlationResult = DetermineMessageCorrelation(message))
            {
                AzureServiceBusMessageContext messageContext = message.GetMessageContext(JobId, Settings.ServiceBusEntity);
                ServiceBusReceiver receiver = args.GetServiceBusReceiver();

                await _messageRouter.RouteMessageAsync(receiver, args.Message, messageContext, correlationResult.CorrelationInfo, args.CancellationToken); 
            }
        }

        private MessageCorrelationResult DetermineMessageCorrelation(ServiceBusReceivedMessage message)
        {
            if (Settings.Options.Routing.Correlation.Format is MessageCorrelationFormat.W3C)
            {
                (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();
                var client = ServiceProvider.GetRequiredService<TelemetryClient>();
                return MessageCorrelationResult.Create(client, transactionId, operationParentId);
            }

            MessageCorrelationInfo correlationInfo = 
                message.GetCorrelationInfo(
                    Settings.Options.Routing.Correlation?.TransactionIdPropertyName ?? PropertyNames.TransactionId,
                    Settings.Options.Routing.Correlation?.OperationParentIdPropertyName ?? PropertyNames.OperationParentId);

            return MessageCorrelationResult.Create(correlationInfo);
        }

        private static async Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == false)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
    }
}
