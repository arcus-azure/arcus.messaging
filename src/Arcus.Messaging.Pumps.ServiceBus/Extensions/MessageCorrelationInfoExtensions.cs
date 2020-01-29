using System;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.ServiceBus.Extensions
{
    /// <summary>
    /// Azure ServiceBus plugin to add correlation information to the inbound and outbound messages.
    /// </summary>
    public static class MessageCorrelationInfoExtensions
    {
        /// <summary>
        /// Append the <see cref="MessageCorrelationInfo"/> after the message is received.
        /// </summary>
        /// <param name="message">The received message.</param>
        public static MessageCorrelationInfo GetCorrelationInfo(this Message message)
        {
            return GetCorrelationInfo(message, NullLogger.Instance);
        }

        /// <summary>
        /// Append the <see cref="MessageCorrelationInfo"/> after the message is received.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="logger">The logger to write diagnostic trace messages while constructing the correlation from the message's information.</param>
        public static MessageCorrelationInfo GetCorrelationInfo(this Message message, ILogger logger)
        {
            Guard.NotNull(message, nameof(message));
            logger ??= NullLogger.Instance;

            string transactionId = DetermineTransactionId(message);
            string operationId = DetermineOperationId(message.CorrelationId, logger);

            var messageCorrelationInfo = new MessageCorrelationInfo(transactionId, operationId);
            logger.LogInformation(
                "Received message '{MessageId}' (Transaction: {TransactionId}, Operation: {OperationId}, Cycle: {CycleId})",
                message.MessageId, messageCorrelationInfo.TransactionId, messageCorrelationInfo.OperationId, messageCorrelationInfo.CycleId);

            return messageCorrelationInfo;
        }

        private static string DetermineOperationId(string messageCorrelationId, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(messageCorrelationId))
            {
                var generatedOperationId = Guid.NewGuid().ToString();
                logger.LogInformation("Generating operation id {OperationId} given no correlation id was found on the message", generatedOperationId);

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
