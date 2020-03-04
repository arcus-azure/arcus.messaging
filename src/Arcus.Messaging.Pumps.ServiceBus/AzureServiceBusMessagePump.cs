using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    public class AzureServiceBusMessagePump : MessagePump
    {
        private bool _isHostShuttingDown;
        private MessageReceiver _messageReceiver;
        private readonly MessageHandlerOptions _messageHandlerOptions;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        public AzureServiceBusMessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<AzureServiceBusMessagePump> logger)
            : base(configuration, serviceProvider, logger)
        {
            Settings = serviceProvider.GetRequiredService<AzureServiceBusMessagePumpSettings>();
            JobId = Settings.Options.JobId;

            SubscriptionName = Settings.SubscriptionName;
            _messageHandlerOptions = DetermineMessageHandlerOptions(Settings);
        }

        /// <summary>
        ///     Path of the entity to process
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
                    Logger.LogTrace(
                        "[Job: {JobId}] Topic subscription with name '{SubscriptionName}' already exists on Service Bus resource",
                        JobId, SubscriptionName);
                }
                else
                {
                    Logger.LogTrace(
                        "[Job: {JobId}] Creating subscription '{SubscriptionName}' on topic '{TopicPath}'...",
                        JobId, SubscriptionName, serviceBusConnectionString.EntityPath);
                    
                    var subscriptionDescription = new SubscriptionDescription(serviceBusConnectionString.EntityPath, SubscriptionName)
                    {
                        UserMetadata = $"Subscription created by Arcus job: '{JobId}' to process Service Bus messages."
                    };
                    var ruleDescription = new RuleDescription("Accept-All", new TrueFilter());
                    await serviceBusClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription, cancellationToken)
                                          .ConfigureAwait(continueOnCapturedContext: false);
                    Logger.LogTrace(
                        "[Job: {JobId}] Subscription '{SubscriptionName}' created on topic '{TopicPath}'",
                        JobId, SubscriptionName, serviceBusConnectionString.EntityPath);
                }
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

                Logger.LogInformation("Starting message pump {MessagePumpId} on entity path '{EntityPath}' in namespace '{Namespace}'", Id, EntityPath, Namespace);

                _messageReceiver.RegisterMessageHandler(HandleMessageAsync, _messageHandlerOptions);
                Logger.LogInformation("Message pump {MessagePumpId} started", Id);

                await UntilCancelledAsync(stoppingToken);

                Logger.LogInformation("Closing message pump {MessagePumpId}", Id);
                await _messageReceiver.CloseAsync();
                Logger.LogInformation("Message pump {MessagePumpId} closed : {Time}", Id, DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                await HandleReceiveExceptionAsync(exception);
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
                throw new InvalidOperationException("Message receiver is not initialized yet.");
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
                throw new InvalidOperationException("Message receiver is not initialized yet.");
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
                throw new InvalidOperationException("Message receiver is not initialized yet.");
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
                throw new InvalidOperationException("Message receiver is not initialized yet.");
            }

            await _messageReceiver.DeadLetterAsync(lockToken, reason, errorDescription);
        }

        private MessageHandlerOptions DetermineMessageHandlerOptions(AzureServiceBusMessagePumpSettings messagePumpSettings)
        {
            var messageHandlerOptions = new MessageHandlerOptions(exceptionReceivedEventArgs => HandleReceiveExceptionAsync(exceptionReceivedEventArgs.Exception));
            if (messagePumpSettings.Options != null)
            {
                // Assign the configured defaults
                messageHandlerOptions.AutoComplete = messagePumpSettings.Options.AutoComplete;
                messageHandlerOptions.MaxConcurrentCalls = messagePumpSettings.Options.MaxConcurrentCalls ?? messageHandlerOptions.MaxConcurrentCalls;

                Logger.LogInformation("Message pump options were configured instead of Azure Service Bus defaults.");
            }
            else
            {
                Logger.LogWarning("No message pump options were configured, using Azure Service Bus defaults instead.");
            }

            return messageHandlerOptions;
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
        }

        private async Task DeleteTopicSubscriptionAsync(CancellationToken cancellationToken)
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
                    Logger.LogTrace(
                        "[Job: {JobId}] Deleting subscription '{SubscriptionName}' on topic '{Path}'...",
                        JobId, SubscriptionName, serviceBusConnectionString.EntityPath);
                    await serviceBusClient.DeleteSubscriptionAsync(serviceBusConnectionString.EntityPath, SubscriptionName, cancellationToken);
                    Logger.LogTrace(
                        "[Job: {JobId}] Subscription '{SubscriptionName}' deleted on topic '{Path}'",
                        JobId, SubscriptionName, serviceBusConnectionString.EntityPath);
                }
                else
                {
                    Logger.LogTrace(
                        "[Job: {JobId}] Cannot delete topic subscription with name '{SubscriptionName}' because no subscription exists on Service Bus resource",
                        JobId, SubscriptionName);
                }
            }
            finally
            {
                await serviceBusClient.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task<ServiceBusConnectionStringBuilder> GetServiceBusConnectionStringAsync()
        {
            Logger.LogTrace("[Job: {JobId}] Getting Azure Service Bus Topic connection string on topic '{TopicPath}'...", JobId, Settings.EntityName);
            string connectionString = await Settings.GetConnectionStringAsync();
            var serviceBusConnectionBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            Logger.LogTrace("[JobId: {JobId}] Got Azure Service Bus Topic connection string on topic '{TopicPath}'", JobId, Settings.EntityName);

            return serviceBusConnectionBuilder;
        }

        private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                Logger.LogWarning("Received message was null, skipping.");
                return;
            }

            if (_isHostShuttingDown)
            {
                Logger.LogWarning("Abandoning message with ID '{MessageId}' as the host is shutting down.", message.MessageId);
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
                Logger.LogInformation(
                    "Received message '{MessageId}' (Transaction: {TransactionId}, Operation: {OperationId}, Cycle: {CycleId})",
                    message.MessageId, correlationInfo.TransactionId, correlationInfo.OperationId, correlationInfo.CycleId);
                
                var messageContext = new AzureServiceBusMessageContext(message.MessageId, message.SystemProperties,
                    message.UserProperties);

                Encoding encoding = messageContext.GetMessageEncodingProperty(Logger);
                string messageBody = encoding.GetString(message.Body);

                await ProcessMessageAsync(messageBody, messageContext, correlationInfo, cancellationToken);

                Logger.LogInformation("Message {MessageId} processed", message.MessageId);
            }
            catch (Exception ex)
            {
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
