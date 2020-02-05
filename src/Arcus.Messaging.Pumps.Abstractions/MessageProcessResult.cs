namespace Arcus.Messaging.Pumps.Abstractions {
    /// <summary>
    /// The status of the message after it was handled by an <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
    /// </summary>
    public enum MessageProcessResult
    {
        /// <summary>
        /// Sets the message as correctly processed.
        /// </summary>
        Processed,

        /// <summary>
        /// Sets the message as not supported by this message handler.
        /// </summary>
        NotSupported,

        /// <summary>
        /// Sets the message as failed during processing by this message handler.
        /// </summary>
        Failure
    }
}