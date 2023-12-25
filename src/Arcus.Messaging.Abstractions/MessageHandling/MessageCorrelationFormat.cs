namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the message correlation format of the received message.
    /// </summary>
    public enum MessageCorrelationFormat
    {
        /// <summary>
        /// Uses the W3C message correlation system with traceparent and tracestate to represent parent-child relationship.
        /// </summary>
        W3C,

        /// <summary>
        /// Uses the hierarchical message correlation system with Root-Id and Request-Id to represent parent-child relationship.
        /// </summary>
        Hierarchical
    }
}