using System;

namespace Arcus.Messaging.ServiceBus.Telemetry.Serilog
{
    /// <summary>
    /// Represents the consumer configurable options model to change the behavior of the registered Serilog message correlation system.
    /// </summary>
    public class SerilogMessageCorrelationOptions
    {
        /// <summary>
        /// Gets the options to control the Serilog <see cref="SerilogMessageCorrelationInfoEnricher"/> when the incoming message is routed via the message router.
        /// </summary>
        public SerilogMessageCorrelationEnricherOptions Enricher { get; } = new();
    }

    /// <summary>
    /// Represents the consumer configurable options model to change the behavior of the Serilog <see cref="SerilogMessageCorrelationInfoEnricher"/>.
    /// </summary>
    public class SerilogMessageCorrelationEnricherOptions
    {
        private string _cycleIdPropertyName = "CycleId",
                       _operationIdPropertyName = "OperationId",
                       _transactionIdPropertyName = "TransactionId",
                       _operationParentIdPropertyName = "OperationParentId";

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
