using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus.Correlation
{
    /// <summary>
    /// Azure ServiceBus plugin to add correlation information to the inbound and outbound messages.
    /// </summary>
    public class MessageCorrelationServiceBusPlugin : ServiceBusPlugin
    {
        private readonly ILogger<MessageCorrelationServiceBusPlugin> _logger;

        /// <summary>
        /// Gets the user property name that will be included in the Service Bus <see cref="Message"/>.
        /// </summary>
        public const string CorrelationInfoUserProperty = nameof(MessageCorrelationInfo);

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationServiceBusPlugin"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public MessageCorrelationServiceBusPlugin(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider));
            
            _logger = serviceProvider.GetRequiredService<ILogger<MessageCorrelationServiceBusPlugin>>();
        }

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public override string Name => nameof(MessageCorrelationServiceBusPlugin);

        /// <summary>
        /// Append the <see cref="MessageCorrelationInfo"/> after the message is received.
        /// </summary>
        /// <param name="message">The received message.</param>
        public override Task<Message> AfterMessageReceive(Message message)
        {
            string transactionId = DetermineTransactionId(message);
            string operationId = DetermineOperationId(message.CorrelationId);

            var messageCorrelationInfo = new MessageCorrelationInfo(transactionId, operationId);
            _logger.LogInformation(
                "Received message '{MessageId}' (Transaction: {TransactionId}, Operation: {OperationId}, Cycle: {CycleId})",
                message.MessageId, messageCorrelationInfo.TransactionId, messageCorrelationInfo.OperationId, messageCorrelationInfo.CycleId);

            message.UserProperties[nameof(MessageCorrelationInfo)] = messageCorrelationInfo;
            return Task.FromResult(message);
        }

        private string DetermineOperationId(string messageCorrelationId)
        {
            if (string.IsNullOrWhiteSpace(messageCorrelationId))
            {
                var generatedOperationId = Guid.NewGuid().ToString();
                _logger.LogInformation("Generating operation id {OperationId} given no correlation id was found on the message", generatedOperationId);

                return generatedOperationId;
            }

            return messageCorrelationId;
        }

        private static string DetermineTransactionId(Message message)
        {
            return message.UserProperties.TryGetValue(PropertyNames.TransactionId, out object transactionId)
                ? transactionId.ToString()
                : string.Empty;
        }
    }
}
