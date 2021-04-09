using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Telemetry;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    public class AzureServiceBusMessagePump : MessagePump
    {
        private readonly IAzureServiceBusFallbackMessageHandler _fallbackMessageHandler;
        private readonly MessageHandlerOptions _messageHandlerOptions;
        private readonly IDisposable _loggingScope;
        
        private bool _isHostShuttingDown;
        private MessageReceiver _messageReceiver;
        private int _unauthorizedExceptionCount = 0;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="settings">Settings to configure the message pump</param>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        public AzureServiceBusMessagePump(
            AzureServiceBusMessagePumpSettings settings, 
            IConfiguration configuration, 
            IServiceProvider serviceProvider, 
            ILogger<AzureServiceBusMessagePump> logger)
            : base(configuration, serviceProvider, logger)
        {
            Guard.NotNull(settings, nameof(settings), "Requires a set of settings to correctly configure the message pump");
            
            Settings = settings;
            JobId = Settings.Options.JobId;
            SubscriptionName = Settings.SubscriptionName;

            _fallbackMessageHandler = serviceProvider.GetService<IAzureServiceBusFallbackMessageHandler>();
            _messageHandlerOptions = DetermineMessageHandlerOptions(Settings);
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
        /// Gets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Gets the name of the topic subscription; combined from the <see cref="AzureServiceBusMessagePumpSettings.SubscriptionName"/> and the <see cref="JobId"/>.
        /// </summary>
        protected string SubscriptionName { get; }

        /// <summary>
        /// Gets the Azure Service Bus message receiver that this message pump uses.
        /// </summary>
        internal MessageReceiver MessageReceiver => _messageReceiver;

        /// <summary>
        /// Reconfigure the Azure Service Bus options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        public void ReconfigureOptions(Action<AzureServiceBusMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus options");

            var options = new AzureServiceBusMessagePumpOptions
            {
                AutoComplete = Settings.Options.AutoComplete,
                JobId = Settings.Options.JobId,
                KeyRotationTimeout = Settings.Options.KeyRotationTimeout,
                MaxConcurrentCalls = Settings.Options.MaxConcurrentCalls,
                MaximumUnauthorizedExceptionsBeforeRestart = Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart
            };

            reconfigure(options);
            Settings.Options = new AzureServiceBusMessagePumpConfiguration(options);
        }

        /// <summary>
        /// Reconfigure the Azure Service Bus Queue options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus Queue options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the message pump is not configured for Queues.</exception>
        public void ReconfigureQueueOptions(Action<AzureServiceBusQueueMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus Queue options");
            Guard.For<NotSupportedException>(
                () => Settings.ServiceBusEntity is ServiceBusEntityType.Topic, 
                "Requires the message pump to be configured for Azure Service Bus Queue to reconfigure these options, use the Topic overload instead");

            var options = new AzureServiceBusQueueMessagePumpOptions
            {
                AutoComplete = Settings.Options.AutoComplete,
                JobId = Settings.Options.JobId,
                KeyRotationTimeout = Settings.Options.KeyRotationTimeout,
                MaxConcurrentCalls = Settings.Options.MaxConcurrentCalls,
                MaximumUnauthorizedExceptionsBeforeRestart = Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart
            };

            reconfigure(options);
            Settings.Options = new AzureServiceBusMessagePumpConfiguration(options);
        }

        /// <summary>
        /// Reconfigure the Azure Service Bus Topic options on this message pump.
        /// </summary>
        /// <param name="reconfigure">The function to reconfigure the Azure Service Bus Topic options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reconfigure"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the message pump is not configured for Topics.</exception>
        public void ReconfigureTopicOptions(Action<AzureServiceBusTopicMessagePumpOptions> reconfigure)
        {
            Guard.NotNull(reconfigure, nameof(reconfigure), "Requires a function to reconfigure the Azure Service Bus Topics options");
            Guard.For<NotSupportedException>(
                () => Settings.ServiceBusEntity is ServiceBusEntityType.Queue,
                "Requires a message pump to be configured for Azure Service Bus Topic to reconfigure these options, use the Queue overload instead");

            var options = new AzureServiceBusTopicMessagePumpOptions
            {
                AutoComplete = Settings.Options.AutoComplete,
                JobId = Settings.Options.JobId,
                MaxConcurrentCalls = Settings.Options.MaxConcurrentCalls,
                KeyRotationTimeout = Settings.Options.KeyRotationTimeout,
                MaximumUnauthorizedExceptionsBeforeRestart = Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart,
                TopicSubscription = Settings.Options.TopicSubscription
            };

            reconfigure(options);
            Settings.Options = new AzureServiceBusMessagePumpConfiguration(options);
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Settings.ServiceBusEntity == ServiceBusEntityType.Topic
                && Settings.Options.TopicSubscription.HasFlag(TopicSubscription.CreateOnStart))
            {
                await CreateTopicSubscriptionAsync(cancellationToken);
            }

            await base.StartAsync(cancellationToken);
        }

        private async Task CreateTopicSubscriptionAsync(CancellationToken cancellationToken)
        {
            ServiceBusConnectionStringBuilder serviceBusConnectionString = await GetServiceBusConnectionStringAsync();
            var serviceBusClient = new ManagementClient(serviceBusConnectionString);

            try
            {
                bool subscriptionExists =
                   await serviceBusClient.SubscriptionExistsAsync(
                       serviceBusConnectionString.EntityPath, SubscriptionName, cancellationToken);
                if (subscriptionExists)
                {
                    Logger.LogTrace("Topic subscription with name '{SubscriptionName}' already exists on Service Bus resource",
                         SubscriptionName);
                }
                else
                {
                    Logger.LogTrace(
                        "Creating subscription '{SubscriptionName}' on topic '{TopicPath}'...",
                         SubscriptionName, serviceBusConnectionString.EntityPath);
                    
                    var subscriptionDescription = new SubscriptionDescription(serviceBusConnectionString.EntityPath, SubscriptionName)
                    {
                        UserMetadata = $"Subscription created by Arcus job: '{JobId}' to process Service Bus messages."
                    };
                    var ruleDescription = new RuleDescription("Accept-All", new TrueFilter());
                    await serviceBusClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription, cancellationToken)
                                          .ConfigureAwait(continueOnCapturedContext: false);
                    Logger.LogTrace(
                        "Subscription '{SubscriptionName}' created on topic '{TopicPath}'",
                         SubscriptionName, serviceBusConnectionString.EntityPath);
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, 
                    "Failed to create topic subscription with name '{SubscriptionName}' on Service Bus resource", 
                     SubscriptionName);
            }
            finally
            {
                await serviceBusClient.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await OpenNewMessageReceiverAsync();
                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occurred during processing of messages");
                await HandleReceiveExceptionAsync(exception);
            }
            finally
            {
                await CloseMessageReceiverAsync();
            }
        }

        private async Task OpenNewMessageReceiverAsync()
        {
            _messageReceiver = await CreateMessageReceiverAsync(Settings);

            Logger.LogTrace("Starting message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}'", JobId, EntityPath, Namespace);
            _messageReceiver.RegisterMessageHandler(HandleMessageAsync, _messageHandlerOptions);
            Logger.LogInformation("Message pump '{JobId}' on entity path '{EntityPath}' in namespace '{Namespace}' started", JobId, EntityPath, Namespace);
        }

        private async Task CloseMessageReceiverAsync()
        {
            if (_messageReceiver is null)
            {
                return;
            }

            try
            {
                Logger.LogTrace("Closing message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}'",  JobId, EntityPath, Namespace);
                await _messageReceiver.CloseAsync();
                Logger.LogInformation("Message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' closed : {Time}",  JobId, EntityPath, Namespace, DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Cannot correctly close the message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}'",  JobId, EntityPath, Namespace);
            }
        }

        /// <summary>
        ///     Marks a message as completed
        /// </summary>
        /// <remarks>This should only be called if <see cref="AzureServiceBusMessagePumpConfiguration.AutoComplete" /> is disabled</remarks>
        /// <param name="lockToken">
        ///     Token used to lock an individual message for processing. See
        ///     <see cref="AzureServiceBusMessageContext.LockToken" />
        /// </param>
        protected virtual async Task CompleteMessageAsync(string lockToken)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            if (_messageReceiver == null)
            {
                throw new InvalidOperationException("Message receiver is not initialized yet");
            }

            await _messageReceiver.CompleteAsync(lockToken);
        }

        /// <summary>
        ///     Abandons the current message that is being processed
        /// </summary>
        /// <param name="lockToken">
        ///     Token used to lock an individual message for processing. See
        ///     <see cref="AzureServiceBusMessageContext.LockToken" />
        /// </param>
        /// <param name="messageProperties">Collection of message properties to include and/or modify</param>
        protected virtual async Task AbandonMessageAsync(string lockToken,
            IDictionary<string, object> messageProperties = null)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            if (_messageReceiver == null)
            {
                throw new InvalidOperationException("Message receiver is not initialized yet");
            }

            await _messageReceiver.AbandonAsync(lockToken, messageProperties);
        }

        /// <summary>
        ///     Deadletters the current message
        /// </summary>
        /// <param name="lockToken">
        ///     Token used to lock an individual message for processing. See
        ///     <see cref="AzureServiceBusMessageContext.LockToken" />
        /// </param>
        /// <param name="messageProperties">Collection of message properties to include and/or modify</param>
        protected virtual async Task DeadletterMessageAsync(string lockToken,
            IDictionary<string, object> messageProperties = null)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            if (_messageReceiver == null)
            {
                throw new InvalidOperationException("Message receiver is not initialized yet");
            }

            await _messageReceiver.DeadLetterAsync(lockToken, messageProperties);
        }

        /// <summary>
        ///     Deadletters the current message
        /// </summary>
        /// <param name="lockToken">
        ///     Token used to lock an individual message for processing. See
        ///     <see cref="AzureServiceBusMessageContext.LockToken" />
        /// </param>
        /// <param name="reason">Reason why it's being deadlettered</param>
        /// <param name="errorDescription">Description related to the error</param>
        protected virtual async Task DeadletterMessageAsync(string lockToken, string reason, string errorDescription)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            if (_messageReceiver == null)
            {
                throw new InvalidOperationException("Message receiver is not initialized yet");
            }

            await _messageReceiver.DeadLetterAsync(lockToken, reason, errorDescription);
        }

        private MessageHandlerOptions DetermineMessageHandlerOptions(AzureServiceBusMessagePumpSettings messagePumpSettings)
        {
            var messageHandlerOptions = new MessageHandlerOptions(async exceptionReceivedEventArgs =>
            {
                try
                {
                    await HandleReceiveExceptionAsync(exceptionReceivedEventArgs.Exception);
                }
                finally
                {
                    if (exceptionReceivedEventArgs.Exception is UnauthorizedException)
                    {
                        if (Interlocked.Increment(ref _unauthorizedExceptionCount) >= Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart)
                        {
                            Logger.LogWarning("Unable to connect anymore to Azure Service Bus, trying to re-authenticate...");
                            await RestartAsync();
                        }
                        else
                        {
                            Logger.LogWarning(
                                "Unable to connect anymore to Azure Service Bus ({CurrentCount}/{MaxCount})", 
                                _unauthorizedExceptionCount, Settings.Options.MaximumUnauthorizedExceptionsBeforeRestart);
                        }
                    }
                }
            });

            if (messagePumpSettings.Options != null)
            {
                // Assign the configured defaults
                messageHandlerOptions.AutoComplete = messagePumpSettings.Options.AutoComplete;
                messageHandlerOptions.MaxConcurrentCalls = messagePumpSettings.Options.MaxConcurrentCalls ?? messageHandlerOptions.MaxConcurrentCalls;

                Logger.LogInformation("Message pump options were configured instead of Azure Service Bus defaults");
            }
            else
            {
                Logger.LogWarning("No message pump options were configured, using Azure Service Bus defaults instead");
            }

            return messageHandlerOptions;
        }

        /// <summary>
        /// Restart core functionality of the message pump.
        /// </summary>
        public async Task RestartAsync()
        {
            Interlocked.Exchange(ref _unauthorizedExceptionCount, 0);

            Logger.LogTrace("Restarting Azure Service Bus message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' ...", JobId, EntityPath, Namespace);
            await CloseMessageReceiverAsync();
            await OpenNewMessageReceiverAsync();
            Logger.LogInformation("Azure Service Bus message pump '{JobId}' on entity path '{EntityPath}' in '{Namespace}' restarted!", JobId, EntityPath, Namespace);
        }

        private async Task<MessageReceiver> CreateMessageReceiverAsync(AzureServiceBusMessagePumpSettings messagePumpSettings)
        {
            var rawConnectionString = await messagePumpSettings.GetConnectionStringAsync();
            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(rawConnectionString);

            MessageReceiver messageReceiver;
            if (string.IsNullOrWhiteSpace(serviceBusConnectionStringBuilder.EntityPath))
            {
                // Connection string doesn't include the entity so we're using the message pump settings
                if (string.IsNullOrWhiteSpace(messagePumpSettings.EntityName))
                {
                    throw new ArgumentException("No entity name was specified while the connection string is scoped to the namespace");
                }

                messageReceiver = CreateReceiver(serviceBusConnectionStringBuilder, messagePumpSettings.EntityName, SubscriptionName);
            }
            else
            {
                // Connection string includes the entity so we're using that instead of the message pump settings
                messageReceiver = CreateReceiver(serviceBusConnectionStringBuilder, serviceBusConnectionStringBuilder.EntityPath, SubscriptionName);
            }

            Namespace = messageReceiver.ServiceBusConnection?.Endpoint?.Host;

            ConfigurePlugins();

            RegisterClientInformation(messageReceiver.ClientId, messageReceiver.Path);

            return messageReceiver;
        }

        private static MessageReceiver CreateReceiver(ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder, string entityName, string subscriptionName)
        {
            var entityPath = entityName;

            if (string.IsNullOrWhiteSpace(subscriptionName) == false)
            {
                entityPath = $"{entityPath}/subscriptions/{subscriptionName}";
            }

            var connectionString = serviceBusConnectionStringBuilder.GetNamespaceConnectionString();
            return new MessageReceiver(connectionString, entityPath);
        }

        private void ConfigurePlugins()
        {
            IEnumerable<ServiceBusPlugin> registeredPlugins = DefineServiceBusPlugins();
            if (registeredPlugins != null)
            {
                foreach (var plugin in registeredPlugins)
                {
                    if (plugin != null)
                    {
                        _messageReceiver.RegisterPlugin(plugin);
                    }
                }
            }
        }

        /// <summary>
        ///     Provides capability to define Service Bus plugins to register
        /// </summary>
        /// <remarks>All Service Bus plugins will be registered in the same order</remarks>
        /// <returns>List of Service Bus plugins that will be used by the message pump</returns>
        protected virtual IEnumerable<ServiceBusPlugin> DefineServiceBusPlugins()
        {
            return Enumerable.Empty<ServiceBusPlugin>();
        }

        /// <summary>
        ///     Triggered when the Azure Service Bus message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Settings.ServiceBusEntity == ServiceBusEntityType.Topic
                && Settings.Options.TopicSubscription.HasFlag(TopicSubscription.DeleteOnStop))
            {
                await DeleteTopicSubscriptionAsync(cancellationToken);
            }

            await base.StopAsync(cancellationToken);
            _isHostShuttingDown = true;
            _loggingScope.Dispose();
        }

        private async Task DeleteTopicSubscriptionAsync(CancellationToken cancellationToken)
        {
            ServiceBusConnectionStringBuilder serviceBusConnectionString = await GetServiceBusConnectionStringAsync();
            var serviceBusClient = new ManagementClient(serviceBusConnectionString);

            try
            {
                bool subscriptionExists = await serviceBusClient.SubscriptionExistsAsync(serviceBusConnectionString.EntityPath, SubscriptionName, cancellationToken);
                if (subscriptionExists)
                {
                    Logger.LogTrace("Deleting subscription '{SubscriptionName}' on topic '{Path}'...", SubscriptionName, serviceBusConnectionString.EntityPath);
                    await serviceBusClient.DeleteSubscriptionAsync(serviceBusConnectionString.EntityPath, SubscriptionName, cancellationToken);
                    Logger.LogTrace("Subscription '{SubscriptionName}' deleted on topic '{Path}'", SubscriptionName, serviceBusConnectionString.EntityPath);
                }
                else
                {
                    Logger.LogTrace("Cannot delete topic subscription with name '{SubscriptionName}' because no subscription exists on Service Bus resource", SubscriptionName);
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Failed to delete topic subscription with name '{SubscriptionName}' on Service Bus resource", SubscriptionName);
            }
            finally
            {
                await serviceBusClient.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task<ServiceBusConnectionStringBuilder> GetServiceBusConnectionStringAsync()
        {
            Logger.LogTrace("Getting Azure Service Bus Topic connection string on topic '{TopicPath}'...",  Settings.EntityName);
            string connectionString = await Settings.GetConnectionStringAsync();
            var serviceBusConnectionBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            Logger.LogTrace("Got Azure Service Bus Topic connection string on topic '{TopicPath}'",  Settings.EntityName);

            return serviceBusConnectionBuilder;
        }

        private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
        {
            if (message is null)
            {
                Logger.LogWarning("Received message was null, skipping");
                return;
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the host is shutting down",  message.MessageId);
                await AbandonMessageAsync(message.SystemProperties.LockToken);

                return;
            }

            if (String.IsNullOrEmpty(message.CorrelationId))
            {
                Logger.LogTrace("No operation ID was found on the message");
            }

            string transactionIdPropertyName = Settings.Options.Correlation?.TransactionIdPropertyName ?? PropertyNames.TransactionId;
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo(transactionIdPropertyName);
            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            {
                var correlationInfoAccessor = serviceScope.ServiceProvider.GetService<ICorrelationInfoAccessor<MessageCorrelationInfo>>();
                if (correlationInfoAccessor is null)
                {
                    Logger.LogTrace("No message correlation configured");
                    await ProcessMessageWithFallbackAsync(message, cancellationToken, correlationInfo);
                }
                else
                {
                    correlationInfoAccessor.SetCorrelationInfo(correlationInfo);
                    using (LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfoAccessor)))
                    {
                        await ProcessMessageWithFallbackAsync(message, cancellationToken, correlationInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Pre-process the message by setting the necessary values the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
        /// </summary>
        /// <param name="messageHandler">The message handler to be used to process the message.</param>
        /// <param name="messageContext">The message context of the message that will be handled.</param>
        protected override Task PreProcessMessageAsync<TMessageContext>(MessageHandler messageHandler, TMessageContext messageContext)
        {
            Guard.NotNull(messageHandler, nameof(messageHandler), "Requires a message handler instance to pre-process the message");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires a message context to pre-process the message");

            object messageHandlerInstance = messageHandler.GetMessageHandlerInstance();
            Type messageHandlerType = messageHandlerInstance.GetType();

            Logger.LogTrace("Start pre-processing message handler {MessageHandlerType}...", messageHandlerType.Name);
            
            if (messageHandlerInstance is AzureServiceBusMessageHandlerTemplate template 
                && messageContext is AzureServiceBusMessageContext serviceBusMessageContext)
            {
                template.SetLockToken(serviceBusMessageContext.SystemProperties.LockToken);
                template.SetMessageReceiver(_messageReceiver);
            }
            else
            {
                Logger.LogTrace("Nothing to pre-process for message handler type '{MessageHandlerType}'", messageHandlerType.Name);
            }
            
            return Task.CompletedTask;
        }

        private async Task ProcessMessageWithFallbackAsync(Message message, CancellationToken cancellationToken, MessageCorrelationInfo correlationInfo)
        {
            try
            {
                Logger.LogTrace("Received message '{MessageId}'", message.MessageId);

                var messageContext = new AzureServiceBusMessageContext(message.MessageId, JobId, message.SystemProperties, message.UserProperties);
                Encoding encoding = messageContext.GetMessageEncodingProperty(Logger);
                string messageBody = encoding.GetString(message.Body);

                if (_fallbackMessageHandler is null)
                {
                    await ProcessMessageAsync(messageBody, messageContext, correlationInfo, cancellationToken);
                }
                else
                {
                    await ProcessMessageWithPotentialFallbackAsync(message, messageBody, messageContext, correlationInfo, cancellationToken);
                }

                Logger.LogTrace("Message '{MessageId}' processed", message.MessageId);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unable to process message with ID '{MessageId}'",  message.MessageId);
                await HandleReceiveExceptionAsync(exception);

                throw;
            }
        }

        private async Task ProcessMessageWithPotentialFallbackAsync(
            Message message,
            string messageBody,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            if (_fallbackMessageHandler is AzureServiceBusMessageHandlerTemplate specificMessageHandler)
            {
                specificMessageHandler.SetMessageReceiver(_messageReceiver);
            }

            var isProcessed = await ProcessMessageAndCaptureAsync(messageBody, messageContext, correlationInfo, cancellationToken);
            if (isProcessed == false)
            {
                await _fallbackMessageHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
            }
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
