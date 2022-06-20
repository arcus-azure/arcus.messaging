﻿using Arcus.Messaging.Abstractions.Telemetry;
using Arcus.Observability.Telemetry.Serilog.Enrichers.Configuration;

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
        public MessageDeserializationOptions Deserialization { get; } = new MessageDeserializationOptions();

        /// <summary>
        /// Gets the options to control the correlation information upon the receiving of messages in the message router.
        /// </summary>
        public MessageCorrelationOptions Correlation { get; } = new MessageCorrelationOptions();

        /// <summary>
        /// Gets the options to control the Serilog <see cref="MessageCorrelationInfoEnricher"/> when the incoming message is routed via the message router.
        /// </summary>
        public MessageCorrelationEnricherOptions Telemetry { get; } = new MessageCorrelationEnricherOptions();
    }
}
