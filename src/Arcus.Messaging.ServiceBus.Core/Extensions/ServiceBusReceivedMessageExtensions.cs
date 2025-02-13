using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;

#pragma warning disable S1133 // Disable usage of deprecated functionality until v3.0 is released.

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusReceivedMessage"/> to more easily retrieve user and system-related information in a more consumer-friendly manner.
    /// </summary>
    public static class ServiceBusReceivedMessageExtensions
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
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Requires an application property key to retrieve the application property from the received Azure Service Bus message", nameof(key));
            }

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
        /// <remarks>
        ///     This uses the hierarchical correlation system. To use the W3C correlation system,
        ///     use <see cref="IDictionaryExtensions.GetTraceParent(IReadOnlyDictionary{string,object})"/> and <see cref="MessageCorrelationResult"/>.
        /// </remarks>
        /// <param name="message">The received message.</param>
        /// <returns>
        ///     The correlation information wrapped inside an <see cref="MessageCorrelationInfo"/>,
        ///     containing the operation and transaction ID from the received <paramref name="message"/>;
        ///     otherwise both will be generated GUID's.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as the extension on the Azure Service bus message is only used for the deprecated 'Hierarchical' correlation format")]
        public static MessageCorrelationInfo GetCorrelationInfo(this ServiceBusReceivedMessage message)
        {
            return GetCorrelationInfo(message, PropertyNames.TransactionId);
        }

        /// <summary>
        ///     Gets the correlation information for a given <paramref name="message"/>.
        /// </summary>
        /// <remarks>
        ///     This uses the hierarchical correlation system. To use the W3C correlation system,
        ///     use <see cref="IDictionaryExtensions.GetTraceParent(IReadOnlyDictionary{string,object})"/> and <see cref="MessageCorrelationResult"/>.
        /// </remarks>
        /// <param name="message">The received message.</param>
        /// <param name="transactionIdPropertyName">The name of the property to retrieve the transaction ID.</param>
        /// <returns>
        ///     The correlation information wrapped inside an <see cref="MessageCorrelationInfo"/>,
        ///     containing the operation and transaction ID from the received <paramref name="message"/>;
        ///     otherwise both will be generated GUID's.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionIdPropertyName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0 as the extension on the Azure Service bus message is only used for the deprecated 'Hierarchical' correlation format")]
        public static MessageCorrelationInfo GetCorrelationInfo(this ServiceBusReceivedMessage message, string transactionIdPropertyName)
        {
            MessageCorrelationInfo messageCorrelationInfo =
                GetCorrelationInfo(message, transactionIdPropertyName, PropertyNames.OperationParentId);

            return messageCorrelationInfo;
        }

        /// <summary>
        ///     Gets the hierarchical correlation information for a given <paramref name="message"/>.
        /// </summary>
        /// <remarks>
        ///     This uses the hierarchical correlation system. To use the W3C correlation system,
        ///     use <see cref="IDictionaryExtensions.GetTraceParent(IReadOnlyDictionary{string,object})"/> and <see cref="MessageCorrelationResult"/>.
        /// </remarks>
        /// <param name="message">The received message.</param>
        /// <param name="transactionIdPropertyName">The name of the user property to retrieve the transaction ID.</param>
        /// <param name="operationParentIdPropertyName">The name of the user property to retrieve the operation parent ID</param>
        /// <returns>
        ///     The correlation information wrapped inside an <see cref="MessageCorrelationInfo"/>,
        ///     containing the operation and transaction ID from the received <paramref name="message"/>;
        ///     otherwise both will be generated GUID's.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="transactionIdPropertyName"/> or the <paramref name="operationParentIdPropertyName"/> is blank.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as the extension on the Azure Service bus message is only used for the deprecated 'Hierarchical' correlation format")]
        public static MessageCorrelationInfo GetCorrelationInfo(
            this ServiceBusReceivedMessage message,
            string transactionIdPropertyName,
            string operationParentIdPropertyName)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(transactionIdPropertyName))
            {
                throw new ArgumentException("Requires a non-blank property name to retrieve the correlation transaction ID from the received Azure Service Bus message", nameof(transactionIdPropertyName));
            }

            if (string.IsNullOrWhiteSpace(operationParentIdPropertyName))
            {
                throw new ArgumentException("Requires a non-blank property name to retrieve the correlation operation parent ID from the received Azure Service Bus message", nameof(operationParentIdPropertyName));
            }

            string transactionId = DetermineTransactionId(message, transactionIdPropertyName);
            string operationId = DetermineOperationId(message.CorrelationId);
            string operationParentId = message.GetOptionalUserProperty(operationParentIdPropertyName);

            var messageCorrelationInfo = new MessageCorrelationInfo(operationId, transactionId, operationParentId);
            return messageCorrelationInfo;
        }

        [Obsolete("Will be removed in v3.0")]
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
        /// <remarks>
        ///     This uses the hierarchical correlation system. To use the W3C correlation system,
        ///     use <see cref="IDictionaryExtensions.GetTraceParent(IReadOnlyDictionary{string,object})"/> and <see cref="MessageCorrelationResult"/>.
        /// </remarks>
        /// <param name="message">The received message.</param>
        /// <returns>
        ///     The correlation transaction ID for message if an application property could be found with the key <see cref="PropertyNames.TransactionId"/>; <c>null</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as the extension on the Azure Service bus message is only used for the deprecated 'Hierarchical' correlation format")]
        public static string GetTransactionId(this ServiceBusReceivedMessage message)
        {
            return GetTransactionId(message, PropertyNames.TransactionId);
        }

        /// <summary>
        ///     Gets the optional transaction ID that is linked to this <paramref name="message"/>	
        /// </summary>
        /// <remarks>
        ///     This uses the hierarchical correlation system. To use the W3C correlation system,
        ///     use <see cref="IDictionaryExtensions.GetTraceParent(IReadOnlyDictionary{string,object})"/> and <see cref="MessageCorrelationResult"/>.
        /// </remarks>
        /// <param name="message">The received message.</param>
        /// <param name="transactionIdPropertyName">Name of the property to determine the transaction id.</param>
        /// <returns>
        ///     The correlation transaction ID for message if an application property could be found with the key <paramref name="transactionIdPropertyName"/>; <c>null</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionIdPropertyName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0 as the extension on the Azure Service bus message is only used for the deprecated 'Hierarchical' correlation format")]
        public static string GetTransactionId(this ServiceBusReceivedMessage message, string transactionIdPropertyName)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(transactionIdPropertyName))
            {
                throw new ArgumentException("Requires a non-blank property name to retrieve the correlation transaction ID from the received Azure Service Bus message", nameof(transactionIdPropertyName));
            }

            return GetOptionalUserProperty(message, transactionIdPropertyName);
        }

        private static string GetOptionalUserProperty(this ServiceBusReceivedMessage message, string propertyName)
        {
            if (message.ApplicationProperties.TryGetValue(propertyName, out object transactionId))
            {
                return transactionId.ToString();
            }

            return null;
        }
    }
}
