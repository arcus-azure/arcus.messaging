using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusReceivedMessage"/> to more easily retrieve user and system-related information in a more consumer-friendly manner.
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        ///     Gets the application property with a given <paramref name="key"/> for a given <paramref name="message"/>.
        /// </summary>
        /// <typeparam name="TProperty">The type of the value of the property.</typeparam>
        /// <param name="message">The message to get the application property from.</param>
        /// <param name="key">The key in the dictionary to look up the application property.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is blank.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when there's no application property for the provided <paramref name="key"/>.</exception>
        /// <exception cref="InvalidCastException">Thrown when the application property's value cannot be cast to the provided <typeparamref name="TProperty"/> type.</exception>
        public static TProperty GetApplicationProperty<TProperty>(this ServiceBusReceivedMessage message, string key)
        {
            Guard.NotNull(message, nameof(message), "Requires an received Azure Service Bus message to retrieve the application property");
            Guard.NotNullOrWhitespace(key, nameof(key), "Requires an application property key to retrieve the application property from the received Azure Service Bus message");
            
            if (message.ApplicationProperties.TryGetValue(key, out object value))
            {
                if (value is TProperty typed)
                {
                    return typed;
                }

                throw new InvalidCastException(
                    $"The found application property with the key: '{key}' in the Service Bus message was not of the expected type: '{typeof(TProperty).Name}'");
            }

            throw new KeyNotFoundException(
                $"No application property with the key: '{key}' was found in the Service Bus message");
        }

        /// <summary>
        ///     Gets the correlation information for a given <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns>
        ///     The correlation information wrapped inside an <see cref="MessageCorrelationInfo"/>,
        ///     containing the operation and transaction ID from the received <paramref name="message"/>;
        ///     otherwise both will be generated GUID's.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public static MessageCorrelationInfo GetCorrelationInfo(this ServiceBusReceivedMessage message)
        {
            Guard.NotNull(message, nameof(message), "Requires an received Azure Service Bus message to retrieve the correlation information");

            return GetCorrelationInfo(message, PropertyNames.TransactionId);
        }

        /// <summary>
        ///     Gets the correlation information for a given <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="transactionIdPropertyName">Name of the property to determine the transaction id.</param>
        /// <returns>
        ///     The correlation information wrapped inside an <see cref="MessageCorrelationInfo"/>,
        ///     containing the operation and transaction ID from the received <paramref name="message"/>;
        ///     otherwise both will be generated GUID's.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionIdPropertyName"/> is blank.</exception>
        public static MessageCorrelationInfo GetCorrelationInfo(this ServiceBusReceivedMessage message, string transactionIdPropertyName)
        {
            Guard.NotNull(message, nameof(message), "Requires an received Azure Service Bus message to retrieve the correlation information");
            Guard.NotNullOrWhitespace(transactionIdPropertyName, nameof(transactionIdPropertyName), "Requires a non-blank property name to retrieve the correlation transaction ID from the received Azure Service Bus message");

            string transactionId = DetermineTransactionId(message, transactionIdPropertyName);
            string operationId = DetermineOperationId(message.CorrelationId);

            var messageCorrelationInfo = new MessageCorrelationInfo(operationId, transactionId);
            return messageCorrelationInfo;
        }

        private static string DetermineTransactionId(ServiceBusReceivedMessage message, string transactionIdPropertyName)
        {
            string transactionId = GetTransactionId(message, transactionIdPropertyName);
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                string generatedTransactionId = Guid.NewGuid().ToString();
                return generatedTransactionId;
            }

            return transactionId;
        }

        private static string DetermineOperationId(string messageCorrelationId)
        {
            if (string.IsNullOrWhiteSpace(messageCorrelationId))
            {
                var generatedOperationId = Guid.NewGuid().ToString();
                return generatedOperationId;
            }

            return messageCorrelationId;
        }

        /// <summary>	
        ///     Gets the transaction ID that is linked to this <paramref name="message"/>	
        /// </summary>	
        /// <param name="message">The received message.</param>
        /// <returns>
        ///     The correlation transaction ID for message if an application property could be found with the key <see cref="PropertyNames.TransactionId"/>; <c>null</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public static string GetTransactionId(this ServiceBusReceivedMessage message)
        {
            Guard.NotNull(message,nameof(message), "Requires an received Azure Service Bus message to retrieve the correlation transaction ID");

            return GetTransactionId(message, PropertyNames.TransactionId);
        }

        /// <summary>
        ///     Gets the optional transaction ID that is linked to this <paramref name="message"/>	
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="transactionIdPropertyName">Name of the property to determine the transaction id.</param>
        /// <returns>
        ///     The correlation transaction ID for message if an application property could be found with the key <paramref name="transactionIdPropertyName"/>; <c>null</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionIdPropertyName"/> is blank.</exception>
        public static string GetTransactionId(this ServiceBusReceivedMessage message, string transactionIdPropertyName)
        {
            Guard.NotNull(message, nameof(message), "Requires an received Azure Service Bus message to retrieve the correlation transaction ID");
            Guard.NotNullOrWhitespace(transactionIdPropertyName, nameof(transactionIdPropertyName), "Requires a non-blank property name to retrieve the correlation transaction ID from the received Azure Service Bus message");

            if (message.ApplicationProperties.TryGetValue(transactionIdPropertyName, out object transactionId))
            {
                return transactionId.ToString();
            }

            return null;
        }
    }
}
