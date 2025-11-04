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
    }
}
