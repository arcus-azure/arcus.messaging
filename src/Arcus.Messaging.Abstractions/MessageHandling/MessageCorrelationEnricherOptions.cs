using System;
using Arcus.Messaging.Abstractions.Telemetry;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the consumer configurable options model to change the behavior of the Serilog <see cref="MessageCorrelationInfoEnricher"/>.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as W3C will be the only supported correlation format")]
    public class MessageCorrelationEnricherOptions
    {
        private string _cycleIdPropertyName = "CycleId";
        private string _operationIdPropertyName = "OperationId";
        private string _transactionIdPropertyName = "TransactionId";
        private string _operationParentIdPropertyName = "OperationParentId";

        /// <summary>
        /// Gets or sets the property name to enrich the log event with the correlation information operation ID.
        /// </summary>
        /// <exception cref="T:System.ArgumentException">Thrown when the <paramref name="value" /> is blank.</exception>
        public string OperationIdPropertyName
        {
            get => _operationIdPropertyName;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _operationIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the property name to enrich the log event with the correlation information transaction ID.
        /// </summary>
        /// <exception cref="T:System.ArgumentException">Thrown when the <paramref name="value" /> is blank.</exception>
        public string TransactionIdPropertyName
        {
            get => _transactionIdPropertyName;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _transactionIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the property name to enrich the log event with the correlation information parent operation ID.
        /// </summary>
        /// <exception cref="T:System.ArgumentException">Thrown when the <paramref name="value" /> is blank.</exception>
        public string OperationParentIdPropertyName
        {
            get => _operationParentIdPropertyName;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _operationParentIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the property name to enrich the log event with the correlation information cycle ID.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string CycleIdPropertyName
        {
            get => _cycleIdPropertyName;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _cycleIdPropertyName = value;
            }
        }
    }
}
