using System;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents how incoming messages can be routed through registered <see cref="IMessageHandler{TMessage}"/> instances.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as only concrete implementations of message routing will be supported from now on")]
    public interface IMessageRouter
    {
    }
}