namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the consumer-configurable options, as part of the <see cref="MessageRouterOptions"/>, to change the behavior of the <see cref="MessageRouter"/>.
    /// </summary>
    public class MessageDeserializationOptions
    {
        /// <summary>
        /// Gets or sets the flag indicating whether or not the default JSON deserialization should ignore missing members
        /// when trying to match a incoming message with a message handler's message type.
        /// </summary>
        public AdditionalMemberHandling AdditionalMembers { get; set; } = AdditionalMemberHandling.Error;
    }
}