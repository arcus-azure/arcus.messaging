using System;
using Arcus.Observability.Telemetry.Serilog.Enrichers.Configuration;

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
        public SerilogMessageCorrelationEnricherOptions Enricher { get; } = new SerilogMessageCorrelationEnricherOptions();
    }

    /// <summary>
    /// Represents the consumer configurable options model to change the behavior of the Serilog <see cref="SerilogMessageCorrelationInfoEnricher"/>.
    /// </summary>
    public class SerilogMessageCorrelationEnricherOptions : CorrelationInfoEnricherOptions
    {
        private string _cycleIdPropertyName = "CycleId";

        /// <summary>
        /// Gets or sets the property name to enrich the log event with the correlation information cycle ID.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string CycleIdPropertyName
        {
            get => _cycleIdPropertyName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank property name for the cycle ID", nameof(value));
                }

                _cycleIdPropertyName = value;
            }
        }
    }
}
