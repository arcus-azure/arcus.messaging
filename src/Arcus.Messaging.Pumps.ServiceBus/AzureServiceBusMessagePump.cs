using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus.Extensions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
            var connectionString = Configuration.GetValue<string>("ARCUS_SERVICEBUS_QUEUE_CONNECTIONSTRING");

            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);

            var queueClient = new QueueClient(serviceBusConnectionStringBuilder.GetNamespaceConnectionString(), serviceBusConnectionStringBuilder.EntityPath, ReceiveMode.PeekLock);

            Logger.LogInformation("Starting message pump");
            queueClient.RegisterMessageHandler(HandleMessage, HandleReceivedException);
            Logger.LogInformation("Message pump started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Logger.LogInformation("Closing message pump");
            await queueClient.CloseAsync();
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

            var rawMessageBody = Encoding.UTF8.GetString(message.Body);
            Logger.LogInformation("Received message {MessageId} with body {MessageBody} (Transaction: {TransactionId}, Operation: {OperationId}, Cycle: {CycleId})", messageContext.MessageId, rawMessageBody, correlationInfo.TransactionId, correlationInfo.OperationId, correlationInfo.CycleId);

            var order = JsonConvert.DeserializeObject<TMessage>(rawMessageBody);
            if (order != null)
            {
                await ProcessMessageAsync(order, messageContext, correlationInfo, cancellationToken);
            }
            else
            {
                Logger.LogError("Unable to deserialize to message contract {ContractName} for message {MessageBody}", typeof(TMessage), rawMessageBody);
            }

            Logger.LogInformation("Message {MessageId} processed", message.MessageId);
        }
    }
}