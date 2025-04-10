using System;

#pragma warning disable S1133 // Disable usage of deprecated functionality until v3.0 is released.

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the message correlation format of the received message.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as W3C will be the only supported correlation format")]
    public enum MessageCorrelationFormat
    {
        /// <summary>
        /// Uses the W3C message correlation system with traceparent and tracestate to represent parent-child relationship.
        /// </summary>
        W3C,

        /// <summary>
        /// Uses the hierarchical message correlation system with Root-Id and Request-Id to represent parent-child relationship.
        /// </summary>
        [Obsolete("Hierarchical correlation will be removed in v3.0")]
        Hierarchical
    }
}