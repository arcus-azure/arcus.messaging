using System;

#pragma warning disable S1133 // Disable usage of deprecated functionality until v3.0 is released.

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the user-configurable options to control the correlation information tracking during the receiving of the messages in the message router.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as W3C will be the only supported correlation format")]
    public class MessageCorrelationOptions
    {
        /// <summary>
        /// Gets or sets the message correlation format of the received message.
        /// </summary>
        public MessageCorrelationFormat Format { get; set; } = MessageCorrelationFormat.W3C;
    }
}
