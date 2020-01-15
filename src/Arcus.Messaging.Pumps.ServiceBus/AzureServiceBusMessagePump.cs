using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    ///     Message pump for processing messages on an Azure Service Bus entity
    /// </summary>
    /// <typeparam name="TMessage">Type of expected message payload</typeparam>
    public abstract class AzureServiceBusMessagePump<TMessage> : MessagePump<TMessage, AzureServiceBusMessageContext>
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
        protected AzureServiceBusMessagePump(IConfiguration configuration, IServiceProvider serviceProvider,
            ILogger logger)
            : base(configuration, serviceProvider, logger)
        {
            Settings = serviceProvider.GetRequiredService<AzureServiceBusMessagePumpSettings>();

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

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _messageReceiver = await CreateMessageReceiverAsync(Settings);

                Logger.LogInformation("Starting message pump on entity path '{EntityPath}' in namespace '{Namespace}'",
                    EntityPath, Namespace);

                _messageReceiver.RegisterMessageHandler(HandleMessageAsync, _messageHandlerOptions);
                Logger.LogInformation("Message pump started");

                await UntilCancelledAsync(stoppingToken);

                Logger.LogInformation("Closing message pump");
                await _messageReceiver.CloseAsync();
                Logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                await HandleReceiveExceptionAsync(exception);
            }
        }

        /// <summary>
        ///     Marks a message as completed
        /// </summary>
        /// <remarks>This should only be called if <see cref="AzureServiceBusMessagePumpOptions.AutoComplete" /> is disabled</remarks>
        /// <param name="lockToken">
        ///     Token used to lock an individual message for processing. See
        ///     <see cref="AzureServiceBusMessageContext.LockToken" />
        /// </param>
        protected virtual async Task CompleteMessageAsync(string lockToken)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

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
                    throw new ArgumentNullException(nameof(messagePumpSettings.EntityName), "No entity name was specified while the connection string is scoped to the namespace");
                }

                messageReceiver = CreateReceiver(serviceBusConnectionStringBuilder, messagePumpSettings.EntityName, messagePumpSettings.SubscriptionName);
            }
            else
            {
                // Connection string includes the entity so we're using that instead of the message pump settings
                messageReceiver = CreateReceiver(serviceBusConnectionStringBuilder, serviceBusConnectionStringBuilder.EntityPath, messagePumpSettings.SubscriptionName);
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
                    _messageReceiver.RegisterPlugin(plugin);
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
            await base.StopAsync(cancellationToken);
            _isHostShuttingDown = true;
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
                var operationId = DetermineOperationId(message.CorrelationId);
                var correlationInfo = new MessageCorrelationInfo(message.GetTransactionId(), operationId);

                var messageContext = new AzureServiceBusMessageContext(message.MessageId, message.SystemProperties,
                    message.UserProperties);

                Logger.LogInformation(
                    "Received message '{MessageId}' (Transaction: {TransactionId}, Operation: {OperationId}, Cycle: {CycleId})",
                    messageContext.MessageId, correlationInfo.TransactionId, correlationInfo.OperationId,
                    correlationInfo.CycleId);

                // Deserialize the message
                TMessage typedMessageBody = DeserializeJsonMessageBody(message.Body, messageContext);

                // Process the message
                // Note - We are not checking for exceptions here as the pump wil handle those and call our exception handling after which it abandons it
                await ProcessMessageAsync(typedMessageBody, messageContext, correlationInfo, cancellationToken);

                Logger.LogInformation("Message {MessageId} processed", message.MessageId);
            }
            catch (Exception ex)
            {
                await HandleReceiveExceptionAsync(ex);
            }
        }

        private string DetermineOperationId(string messageCorrelationId)
        {
            if (string.IsNullOrWhiteSpace(messageCorrelationId))
            {
                var generatedOperationId = Guid.NewGuid().ToString();
                Logger.LogInformation("Generating operation id {OperationId} given no correlation id was found on the message", generatedOperationId);

                return generatedOperationId;
            }

            return messageCorrelationId;
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