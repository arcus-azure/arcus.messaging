using System;

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
        /// Gets or sets the message correlation format of the received message.
        /// </summary>
        public MessageCorrelationFormat Format { get; set; } = MessageCorrelationFormat.W3C;

        /// <summary>
        /// Gets or sets the name of the message property to retrieve the transaction ID.
        /// </summary>
        /// <remarks>
        ///     Only used when the <see cref="Format"/> is set to <see cref="MessageCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string TransactionIdPropertyName
        {
            get => _transactionIdPropertyName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank property name for the transaction ID", nameof(value));
                }

                _transactionIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the message property to retrieve the operation parent ID.
        /// </summary>
        /// <remarks>
        ///     Only used when the <see cref="Format"/> is set to <see cref="MessageCorrelationFormat.Hierarchical"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string OperationParentIdPropertyName
        {
            get => _operationParentIdPropertyName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank property name for the operation parent ID", nameof(value));
                }

                _operationParentIdPropertyName = value;
            }
        }
    }
}
