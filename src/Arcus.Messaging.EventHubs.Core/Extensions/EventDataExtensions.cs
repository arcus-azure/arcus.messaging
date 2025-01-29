using System;
using Arcus.Messaging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.EventHubs
{
    /// <summary>
    /// Extensions on the <see cref="EventData"/> to retrieve messaging values in a easy manner.
    /// </summary>
    public static class EventDataExtensions
    {
        /// <summary>
        /// Gets the message correlation information from the given <paramref name="eventData"/> in the <see cref="EventData.CorrelationId"/> and <see cref="EventData.Properties"/>,
        /// using the default <see cref="PropertyNames"/>.
        /// </summary>
        /// <remarks>
        ///     The transaction ID will be generated when no such value is present in the <paramref name="eventData"/>'s <see cref="EventData.Properties"/>,
        ///     the operation ID will be generated when no such value is present in the <paramref name="eventData"/>'s <see cref="EventData.CorrelationId"/>.
        /// </remarks>
        /// <param name="eventData">The consumed Azure EventHubs event message to retrieve the message correlation information from.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventData"/> is <c>null</c>.</exception>
        public static MessageCorrelationInfo GetCorrelationInfo(this EventData eventData)
        {
            return GetCorrelationInfo(eventData, PropertyNames.TransactionId);
        }

        /// <summary>
        /// Gets the message correlation information from the given <paramref name="eventData"/> in the <see cref="EventData.CorrelationId"/> and <see cref="EventData.Properties"/>,
        /// using both the <paramref name="transactionIdPropertyName"/> and <see cref="PropertyNames.OperationParentId"/>.
        /// </summary>
        /// <remarks>
        ///     The transaction ID will be generated when no such value is present in the <paramref name="eventData"/>'s <see cref="EventData.Properties"/>,
        ///     the operation ID will be generated when no such value is present in the <paramref name="eventData"/>'s <see cref="EventData.CorrelationId"/>.
        /// </remarks>
        /// <param name="eventData">The consumed Azure EventHubs event message to retrieve the message correlation from.</param>
        /// <param name="transactionIdPropertyName">
        ///     The custom application property name where the transaction ID is stored in the <paramref name="eventData"/>'s <see cref="EventData.Properties"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventData"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionIdPropertyName"/> is blank.</exception>
        public static MessageCorrelationInfo GetCorrelationInfo(
            this EventData eventData,
            string transactionIdPropertyName)
        {
            return GetCorrelationInfo(eventData, transactionIdPropertyName, PropertyNames.OperationParentId);
        }

        /// <summary>
        /// Gets the message correlation information from the given <paramref name="eventData"/> in the <see cref="EventData.CorrelationId"/> and <see cref="EventData.Properties"/>,
        /// using both the <paramref name="transactionIdPropertyName"/> and <paramref name="operationParentIdPropertyName"/>.
        /// </summary>
        /// <remarks>
        ///     The transaction ID will be generated when no such value is present in the <paramref name="eventData"/>'s <see cref="EventData.Properties"/>,
        ///     the operation ID will be generated when no such value is present in the <paramref name="eventData"/>'s <see cref="EventData.CorrelationId"/>.
        /// </remarks>
        /// <param name="eventData">The consumed Azure EventHubs event message to retrieve the message correlation from.</param>
        /// <param name="transactionIdPropertyName">
        ///     The custom application property name where the transaction ID is stored in the <paramref name="eventData"/>'s <see cref="EventData.Properties"/>.
        /// </param>
        /// <param name="operationParentIdPropertyName">
        ///     The custom application property where the transaction ID is stored in the <paramref name="eventData"/>'s <see cref="EventData.Properties"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventData"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="transactionIdPropertyName"/> or the <paramref name="operationParentIdPropertyName"/> is blank.
        /// </exception>
        public static MessageCorrelationInfo GetCorrelationInfo(
            this EventData eventData,
            string transactionIdPropertyName,
            string operationParentIdPropertyName)
        {
            if (eventData is null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (string.IsNullOrWhiteSpace(transactionIdPropertyName))
            {
                throw new ArgumentException("Requires a non-blank application property name to retrieve the transaction ID from the event data's properties", nameof(transactionIdPropertyName));
            }

            if (string.IsNullOrWhiteSpace(operationParentIdPropertyName))
            {
                throw new ArgumentException("Requires a non-blank application property name to retrieve the operation parent ID from the event data's properties", nameof(operationParentIdPropertyName));
            }

            string transactionId = DetermineTransactionId(eventData, transactionIdPropertyName);
            string operationId = DetermineOperationId(eventData.CorrelationId);
            string operationParentId = GetOptionalUserProperty(eventData, operationParentIdPropertyName);

            return new MessageCorrelationInfo(operationId, transactionId, operationParentId);
        }

        private static string DetermineTransactionId(EventData eventData, string transactionIdPropertyName)
        {
            string transactionId = GetOptionalUserProperty(eventData, transactionIdPropertyName);
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                string generatedTransactionId = Guid.NewGuid().ToString();
                return generatedTransactionId;
            }

            return transactionId;
        }

        private static string GetOptionalUserProperty(this EventData eventData, string propertyName)
        {
            if (eventData.Properties.TryGetValue(propertyName, out object propertyValue))
            {
                return propertyValue.ToString();
            }

            return null;
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
    }
}
