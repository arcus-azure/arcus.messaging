using System;
using GuardNet;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents an outcome of a message that was processed by an <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
    /// </summary>
    public class MessageProcessingResult
    {
        private MessageProcessingResult()
        {
            IsSuccessful = true;
        }

        private MessageProcessingResult(Exception processingException)
        {
            Guard.NotNull(processingException, nameof(processingException));

            IsSuccessful = false;
            ProcessingException = processingException;
        }

        /// <summary>
        /// Gets the boolean flag that indicates whether this result represents a successful or unsuccessful outcome of a processed message.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Gets the exception that occurred during the message processing that represents the cause of the processing failure.
        /// </summary>
        /// <remarks>
        ///     Only available when this processing result represents an unsuccessful message processing result - when <see cref="IsSuccessful"/> is <c>false</c>.
        /// </remarks>
        public Exception ProcessingException { get; }

        /// <summary>
        /// Gets an <see cref="MessageProcessingResult"/> instance that represents a result of a message was successfully processed.
        /// </summary>
        public static MessageProcessingResult Success => new MessageProcessingResult();

        /// <summary>
        /// Creates an <see cref="MessageProcessingResult"/> instance that represents a result of a message that was unsuccessfully processed.
        /// </summary>
        /// <param name="processingException">The exception that occurred during the message processing that represents the cause of the processing failure.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="processingException"/> is blank.</exception>
        public static MessageProcessingResult Failure(Exception processingException)
        {
            Guard.NotNull(processingException, nameof(processingException));
            return new MessageProcessingResult(processingException);
        }
    }
}
