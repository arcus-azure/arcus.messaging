using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    public static class MessageExtensions
    {
        /// <summary>
        ///     Gets the user property with a given <paramref name="key"/>
        /// </summary>
        /// <typeparam name="TProperty">The type of the value of the property.</typeparam>
        /// <param name="message">The message to get the user property from.</param>
        /// <param name="key">The key in the dictionary to look up the user property.</param>
        public static TProperty GetUserProperty<TProperty>(this Message message, string key)
        {
            if (message.UserProperties.TryGetValue(key, out object value))
            {
                if (value is TProperty typed)
                {
                    return typed;
                }

                throw new InvalidCastException(
                    $"The found user property with the key: '{key}' in the Service Bus message was not of the expected type: '{typeof(TProperty).Name}'");
            }

            throw new KeyNotFoundException(
                $"No user property with the key: '{key}' was found in the Service Bus message");
        }

        /// <summary>
        ///     Gets the correlation information for a given message.
        /// </summary>
        /// <param name="message">The received message.</param>
        public static MessageCorrelationInfo GetCorrelationInfo(this Message message)
        {
            Guard.NotNull(message, nameof(message));

            string transactionId = GetTransactionId(message);
            string operationId = DetermineOperationId(message.CorrelationId);

            var messageCorrelationInfo = new MessageCorrelationInfo(operationId, transactionId);
            return messageCorrelationInfo;
        }

        private static string DetermineOperationId(string messageCorrelationId)
        {
            if (String.IsNullOrWhiteSpace(messageCorrelationId))
            {
                var generatedOperationId = Guid.NewGuid().ToString();
                return generatedOperationId;
            }

            return messageCorrelationId;
        }

        /// <summary>	
        ///     Gets the transaction id that is linked to this message	
        /// </summary>	
        /// <param name="message">Message to process</param>	
        /// <returns>Transaction id for message</returns>
        public static string GetTransactionId(this Message message)
        {
            return message.UserProperties.TryGetValue(PropertyNames.TransactionId, out object transactionId)
                ? transactionId.ToString()
                : string.Empty;
        }
    }
}
