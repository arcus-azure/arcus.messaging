using System;
using Arcus.Messaging.Abstractions.Telemetry;
using Arcus.Observability.Telemetry.Serilog.Enrichers.Configuration;
using GuardNet;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the consumer configurable options model to change the behavior of the Serilog <see cref="MessageCorrelationInfoEnricher"/>.
    /// </summary>
    public class MessageCorrelationEnricherOptions : CorrelationInfoEnricherOptions
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
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank property name to enrich the log event with the correlation information cycle ID");
                _cycleIdPropertyName = value;
            }
        }
    }
}
