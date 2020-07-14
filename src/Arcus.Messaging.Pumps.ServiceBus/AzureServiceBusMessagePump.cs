using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
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
        private readonly MessageHandlerOptions _messageHandlerOptions;
        private readonly IDisposable _loggingScope;
        
        private bool _isHostShuttingDown;
        private MessageReceiver _messageReceiver;

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

            _messageHandlerOptions = DetermineMessageHandlerOptions(Settings);
            _loggingScope = logger.BeginScope("Job: {JobId}", JobId);
        }

        /// <summary>
        ///     Gets the settings configuring the message pump.
        /// </summary>
        protected AzureServiceBusMessagePumpSettings Settings { get; }

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        protected string Namespace { get; private set; }

        /// <summary>
        /// Gets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Gets the name of the topic subscription; combined from the <see cref="AzureServiceBusMessagePumpSettings.SubscriptionName"/> and the <see cref="JobId"/>.
        /// </summary>
        protected string SubscriptionName { get; }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Settings.ServiceBusEntity == ServiceBusEntity.Topic
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
                _messageReceiver = await CreateMessageReceiverAsync(Settings);

                Logger.LogInformation(
                    "Starting message pump {MessagePumpId} on entity path '{EntityPath}' in namespace '{Namespace}'",
                    Id, EntityPath, Namespace);

                _messageReceiver.RegisterMessageHandler(HandleMessageAsync, _messageHandlerOptions);
                Logger.LogInformation("Message pump {MessagePumpId} started",  Id);

                await UntilCancelledAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unexpected failure occured during processing of messages");
                await HandleReceiveExceptionAsync(exception);
            }
            finally
            {
                if (_messageReceiver != null)
                {
                    await CloseMessageReceiverAsync();
                }
            }
        }

        private async Task CloseMessageReceiverAsync()
        {
            try
            {
                Logger.LogInformation("Closing message pump {MessagePumpId}",  Id);
                await _messageReceiver.CloseAsync();
                Logger.LogInformation("Message pump {MessagePumpId} closed : {Time}",  Id, DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Cannot correctly close the message pump {MessagePumpId}",  Id);
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
                    await ReAuthenticateMessageReceiverAsync(exceptionReceivedEventArgs.Exception);
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

        private async Task ReAuthenticateMessageReceiverAsync(Exception exception)
        {
            if (exception is UnauthorizedException)
            {
                Logger.LogWarning("Unable to connect anymore to Azure Service Bus, trying to re-authenticate...");
                Logger.LogTrace("Restarting Azure Service Bus...");
                
                await CloseMessageReceiverAsync();
                using (var stopCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await StopAsync(stopCancellationTokenSource.Token);
                }

                Logger.LogInformation("Azure Service Bus stopped!");

                using (var startCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await StartAsync(startCancellationTokenSource.Token);
                }

                Logger.LogInformation("Azure Service Bus restarted!");
            }
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
            if (Settings.ServiceBusEntity == ServiceBusEntity.Topic
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
                bool subscriptionExists =
                    await serviceBusClient.SubscriptionExistsAsync(serviceBusConnectionString.EntityPath, SubscriptionName, cancellationToken);
                
                if (subscriptionExists)
                {
                    Logger.LogTrace(
                        "Deleting subscription '{SubscriptionName}' on topic '{Path}'...",
                         SubscriptionName, serviceBusConnectionString.EntityPath);
                    
                    await serviceBusClient.DeleteSubscriptionAsync(serviceBusConnectionString.EntityPath, SubscriptionName, cancellationToken);
                    
                    Logger.LogTrace(
                        "Subscription '{SubscriptionName}' deleted on topic '{Path}'",
                         SubscriptionName, serviceBusConnectionString.EntityPath);
                }
                else
                {
                    Logger.LogTrace(
                        "Cannot delete topic subscription with name '{SubscriptionName}' because no subscription exists on Service Bus resource",
                         SubscriptionName); }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, 
                    "Failed to delete topic subscription with name '{SubscriptionName}' on Service Bus resource", 
                     SubscriptionName);
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
            if (message == null)
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

            try
            {
                if (String.IsNullOrEmpty(message.CorrelationId))
                {
                    Logger.LogInformation("No operation ID was found on the message");
                }


                MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();
                using (IServiceScope serviceScope = ServiceProvider.CreateScope())
                {
                    var correlationInfoAccessor = serviceScope.ServiceProvider.GetService<ICorrelationInfoAccessor<MessageCorrelationInfo>>();
                    correlationInfoAccessor.SetCorrelationInfo(correlationInfo);

                    using (LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfoAccessor)))
                    {
                        Logger.LogInformation("Received message '{MessageId}'", message.MessageId);

                        var messageContext = new AzureServiceBusMessageContext(message.MessageId, message.SystemProperties, message.UserProperties);

                        Encoding encoding = messageContext.GetMessageEncodingProperty(Logger);
                        string messageBody = encoding.GetString(message.Body);

                        await ProcessMessageAsync(messageBody, messageContext, correlationInfo, cancellationToken);

                        Logger.LogInformation("Message {MessageId} processed", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Unable to process message with ID '{MessageId}'",  message.MessageId);
                await HandleReceiveExceptionAsync(ex);
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
