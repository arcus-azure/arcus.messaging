using System;
using Arcus.Messaging.Abstractions.Telemetry;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the consumer-configurable options to change the behavior of the <see cref="MessageRouter"/>.
    /// </summary>
    public class MessageRouterOptions
    {
        /// <summary>
        /// Gets the consumer-configurable options to change the deserialization behavior of the message router.
        /// </summary>
        public MessageDeserializationOptions Deserialization { get; } = new();

        /// <summary>
        /// Gets the consumer configurable options model to change the behavior of the tracked telemetry.
        /// </summary>
        [Obsolete("Will be removed in v3.0 as the correlation is handled outside the core message routing")]
        public MessageTelemetryOptions Telemetry { get; } = new();

        /// <summary>
        /// Gets the options to control the Serilog <see cref="MessageCorrelationInfoEnricher"/> when the incoming message is routed via the message router.
        /// </summary>
        [Obsolete("Will be removed in v3.0 as the correlation is handled outside the core message routing")]
        public MessageCorrelationEnricherOptions CorrelationEnricher { get; } = new();
    }
}
