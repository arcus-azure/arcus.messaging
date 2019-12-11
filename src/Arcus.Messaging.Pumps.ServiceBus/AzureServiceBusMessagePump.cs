using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    public abstract class AzureServiceBusMessagePump<TMessage> : MessagePump<TMessage, AzureServiceBusMessageContext>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="logger">Logger to write telemetry to</param>
        protected AzureServiceBusMessagePump(IConfiguration configuration, ILogger logger)
            : base(configuration, logger)
        {
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
            Logger.LogInformation("Creating message pump");
            MessageReceiver messageReceiver = CreateMessageReceiver();
            Logger.LogInformation("Starting message pump on entity path {EntityPath} in namespace {Namespace}", EntityPath, Namespace);

            // TODO: Message pump options to not delete for example
            var messageHandlerOptions = new MessageHandlerOptions(HandleReceivedException);
            messageReceiver.RegisterMessageHandler(HandleMessage, messageHandlerOptions);
            Logger.LogInformation("Message pump started");

            await UntilCancelledAsync(stoppingToken);

            Logger.LogInformation("Closing message pump");
            await messageReceiver.CloseAsync();
            Logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
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

        private async Task HandleReceivedException(ExceptionReceivedEventArgs exceptionEvent)
        {
            await HandleReceiveExceptionAsync(exceptionEvent.Exception);
        }

        private async Task HandleMessage(Message message, CancellationToken cancellationToken)
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