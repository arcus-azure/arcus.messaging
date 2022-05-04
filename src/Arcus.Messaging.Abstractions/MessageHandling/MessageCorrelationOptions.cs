using System;
using GuardNet;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the user-configurable options to control the correlation information tracking during the receiving of the messages in the message router.
    /// </summary>
    public class MessageCorrelationOptions
    {
        private string _transactionIdPropertyName = PropertyNames.TransactionId,
                       _operationParentIdPropertyName = PropertyNames.OperationParentId;

        /// <summary>
        /// Gets or sets the name of the message property to retrieve the transaction ID.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string TransactionIdPropertyName
        {
            get => _transactionIdPropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Transaction ID message property name for the message cannot be blank");
                _transactionIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the message property to retrieve the operation parent ID.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string OperationParentIdPropertyName
        {
            get => _operationParentIdPropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Operation parent ID message property name for the message cannot be blank");
                _operationParentIdPropertyName = value;
            }
        }
    }
}
