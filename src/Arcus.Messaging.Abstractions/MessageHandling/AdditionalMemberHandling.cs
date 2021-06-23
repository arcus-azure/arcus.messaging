namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a model to determine what to do with additional members in an incoming message payload
    /// during the build-in JSON deserialization in the <see cref="MessageRouter"/>.
    /// </summary>
    public enum AdditionalMemberHandling
    {
        /// <summary>
        /// Makes the built-in JSON deserialization result in an error when any additional members are encountered.
        /// </summary>
        Error = 0,
        
        /// <summary>
        /// Makes the built-in JSON deserialization ignore any additional members that are encountered.
        /// </summary>
        Ignore = 1
    }
}
