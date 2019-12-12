using System;
using System.Collections.Generic;
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
    public abstract class AzureServiceBusMessagePump<TMessage> : MessagePump<TMessage, AzureServiceBusMessageContext>
    {
        private readonly MessageReceiver _messageReceiver;
        private readonly MessageHandlerOptions _messageHandlerOptions;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        protected AzureServiceBusMessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger logger)
            : base(configuration, serviceProvider, logger)
        {
            _messageReceiver = CreateMessageReceiver();
            _messageHandlerOptions = DetermineMessageHandlerOptions();
        }

        /// <summary>
        ///     Path of the entity to process
        /// </summary>
        public string EntityPath { get; private set; }

        /// <summary>
        ///     Service Bus namespace that contains the entity
        /// </summary>
        public string Namespace { get; private set; }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Starting message pump on entity path {EntityPath} in namespace {Namespace}", EntityPath, Namespace);

            _messageReceiver.RegisterMessageHandler(HandleMessageAsync, _messageHandlerOptions);
            Logger.LogInformation("Message pump started");

            await UntilCancelledAsync(stoppingToken);

            Logger.LogInformation("Closing message pump");
            await _messageReceiver.CloseAsync();
            Logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
        }

        /// <summary>
        ///     Marks a message as completed
        /// </summary>
        /// <remarks>This should only be called if <see cref="AzureServiceBusMessagePumpOptions.AutoComplete"/> is disabled</remarks>
        /// <param name="lockToken">Token used to lock an individual message for processing. See <see cref="AzureServiceBusMessageContext.LockToken"/></param>
        protected virtual async Task CompleteMessageAsync(string lockToken)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            await _messageReceiver.CompleteAsync(lockToken);
        }

        /// <summary>
        ///     Deadletters the current message
        /// </summary>
        /// <param name="lockToken">Token used to lock an individual message for processing. See <see cref="AzureServiceBusMessageContext.LockToken"/></param>
        /// <param name="messageProperties">Collection of message properties to include and/or modify</param>
        protected virtual async Task DeadletterMessageAsync(string lockToken, IDictionary<string, object> messageProperties = null)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            await _messageReceiver.DeadLetterAsync(lockToken, messageProperties);
        }

        /// <summary>
        ///     Deadletters the current message
        /// </summary>
        /// <param name="lockToken">Token used to lock an individual message for processing. See <see cref="AzureServiceBusMessageContext.LockToken"/></param>
        /// <param name="reason">Reason why it's being deadlettered</param>
        /// <param name="errorDescription">Description related to the error</param>
        protected virtual async Task DeadletterMessageAsync(string lockToken, string reason, string errorDescription)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            await _messageReceiver.DeadLetterAsync(lockToken, reason, errorDescription);
        }

        /// <summary>
        ///     Abandons the current message that is being processed
        /// </summary>
        /// <param name="lockToken">Token used to lock an individual message for processing. See <see cref="AzureServiceBusMessageContext.LockToken"/></param>
        /// <param name="messageProperties">Collection of message properties to include and/or modify</param>
        protected virtual async Task AbandonMessageAsync(string lockToken, IDictionary<string, object> messageProperties = null)
        {
            Guard.NotNullOrEmpty(lockToken, nameof(lockToken));

            await _messageReceiver.AbandonAsync(lockToken, messageProperties);
        }

        private MessageHandlerOptions DetermineMessageHandlerOptions()
        {
            var messageHandlerOptions = new MessageHandlerOptions(HandleReceivedExceptionAsync);

            var messagePumpOptions = ServiceProvider.GetService<AzureServiceBusMessagePumpOptions>();
            if (messagePumpOptions != null)
            {
                // Assign the configured defaults
                messageHandlerOptions.AutoComplete = messagePumpOptions.AutoComplete;
                messageHandlerOptions.MaxConcurrentCalls = messagePumpOptions.MaxConcurrentCalls ?? messageHandlerOptions.MaxConcurrentCalls;
                Logger.LogInformation("Message pump options were configured instead of Azure Service Bus defaults.");
            }
            else
            {
                Logger.LogWarning("No message pump options were configured, using Azure Service Bus defaults instead.");
            }

            return messageHandlerOptions;
        }

        private MessageReceiver CreateMessageReceiver()
        {
            var connectionString = Configuration.GetValue<string>("ARCUS_SERVICEBUS_QUEUE_CONNECTIONSTRING");

            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);

            var messageReceiver = new MessageReceiver(serviceBusConnectionStringBuilder);

            EntityPath = serviceBusConnectionStringBuilder.EntityPath;
            Namespace = messageReceiver.ServiceBusConnection.Endpoint.Host;

            return messageReceiver;
        }

        private async Task HandleReceivedExceptionAsync(ExceptionReceivedEventArgs exceptionEvent)
        {
            await HandleReceiveExceptionAsync(exceptionEvent.Exception);
        }

        private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message));

            var correlationInfo = new MessageCorrelationInfo(message.GetTransactionId(), message.CorrelationId);
            var messageContext = new AzureServiceBusMessageContext(message.MessageId, message.SystemProperties, message.UserProperties);

            Logger.LogInformation("Received message '{MessageId}' (Transaction: {TransactionId}, Operation: {OperationId}, Cycle: {CycleId})",
                messageContext.MessageId, correlationInfo.TransactionId, correlationInfo.OperationId,
                correlationInfo.CycleId);

            // Deserialize the message
            TMessage typedMessageBody = DeserializeJsonMessageBody(message.Body, messageContext);

            // Process the message
            // Note - We are not checking for exceptions here as the pump wil handle those and call our exception handling after which it abandons it
            await ProcessMessageAsync(typedMessageBody, messageContext, correlationInfo, cancellationToken);

            Logger.LogInformation("Message {MessageId} processed", message.MessageId);
        }

        private static async Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}