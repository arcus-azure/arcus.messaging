using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions;
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO: Make this configurable
            var connectionString = Configuration.GetValue<string>("ARCUS_SERVICEBUS_QUEUE_CONNECTIONSTRING");

            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);

            var messageReceiver = new MessageReceiver(serviceBusConnectionStringBuilder.GetNamespaceConnectionString(), serviceBusConnectionStringBuilder.EntityPath, ReceiveMode.PeekLock);

            Logger.LogInformation("Starting message pump");
            messageReceiver.RegisterMessageHandler(HandleMessage, HandleReceivedException);
            Logger.LogInformation("Message pump started");

            await UntilCancelledAsync(stoppingToken);

            Logger.LogInformation("Closing message pump");
            await messageReceiver.CloseAsync();
            Logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
        }

        private async Task HandleReceivedException(ExceptionReceivedEventArgs exceptionEvent)
        {
            await HandleReceiveExceptionAsync(exceptionEvent.Exception);
        }

        private async Task HandleMessage(Message message, CancellationToken cancellationToken)
        {
            var correlationInfo = new MessageCorrelationInfo(message.GetTransactionId(), message.CorrelationId);
            var messageContext = new AzureServiceBusMessageContext(message.MessageId, message.SystemProperties, message.UserProperties);

            Logger.LogInformation("Received message {MessageId} (Transaction: {TransactionId}, Operation: {OperationId}, Cycle: {CycleId})", messageContext.MessageId, correlationInfo.TransactionId, correlationInfo.OperationId, correlationInfo.CycleId);

            var encoding = DetermineMessageEncoding(messageContext);
            var order = DeserializeMessageBody(message.Body, encoding);
            if (order != null)
            {
                await ProcessMessageAsync(order, messageContext, correlationInfo, cancellationToken);
            }
            else
            {
                Logger.LogError("Unable to deserialize to message contract {ContractName} for message {MessageId}", typeof(TMessage), messageContext.MessageId);
            }

            Logger.LogInformation("Message {MessageId} processed", message.MessageId);
        }

        private static Task UntilCancelledAsync(CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), taskCompletionSource);

            return taskCompletionSource.Task;
        }
    }
}