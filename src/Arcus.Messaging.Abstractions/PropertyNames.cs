namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents general property names within contextual information of processing messages.
    /// </summary>
    public static class PropertyNames
    {
        /// <summary>
        /// Gets the context property name to get the correlation transaction ID.
        /// </summary>
        public const string TransactionId = "Transaction-Id";
        
        /// <summary>
        /// Gets the context property name to get the encoding that was used on the message.
        /// </summary>
        public const string Encoding = "Message-Encoding";
        
        /// <summary>
        /// Gets the context property to get the content type of the message.
        /// </summary>
        public const string ContentType = "Content-Type";
    }
}