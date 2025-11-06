using System;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents all the possible errors of a <see cref="MessageProcessingResult"/>.
    /// </summary>
    public enum MessageProcessingError
    {
        /// <summary>
        /// Defines an error that shows that the message processing was interrupted by some external cause,
        /// unrelated to the message routing.
        /// </summary>
        ProcessingInterrupted,

        /// <summary>
        /// Defines an error shows that no <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation was found
        /// that was able to process the received message.
        /// </summary>
        CannotFindMatchedHandler,

        /// <summary>
        /// Defines and error that shows that the matched <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation
        /// was unable to process the received message.
        /// </summary>
        MatchedHandlerFailed,
    }

    /// <summary>
    /// Represents an outcome of a message that was processed by an <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
    /// </summary>
    public class MessageProcessingResult
    {
        private MessageProcessingResult(string messageId)
        {
            MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
            IsSuccessful = true;
        }

        private MessageProcessingResult(string messageId, MessageProcessingError error, string errorMessage, Exception processingException)
        {
            MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
            Error = error;
            ErrorMessage = errorMessage;
            ProcessingException = processingException;
            IsSuccessful = false;
        }

        /// <summary>
        /// Gets the boolean flag that indicates whether this result represents a successful or unsuccessful outcome of a processed message.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Gets the unique ID to identify the message for which this is a processing result.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Gets the error type that shows which kind of error the message processing failed.
        /// </summary>
        /// <remarks>
        ///     Only available when this processing result represents an unsuccessful message processing result - when <see cref="IsSuccessful"/> is <c>false</c>.
        /// </remarks>
        public MessageProcessingError Error { get; }

        /// <summary>
        /// Gets the description that explains the context of the <see cref="Error"/>.
        /// </summary>
        /// <remarks>
        ///     Only available when this processing result represents an unsuccessful message processing result - when <see cref="IsSuccessful"/> is <c>false</c>.
        /// </remarks>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the exception that occurred during the message processing that represents the cause of the processing failure.
        /// </summary>
        /// <remarks>
        ///     Only possibly available when this processing result represents an unsuccessful message processing result - when <see cref="IsSuccessful"/> is <c>false</c>.
        /// </remarks>
        public Exception ProcessingException { get; }

        /// <summary>
        /// Gets an <see cref="MessageProcessingResult"/> instance that represents a result of a message was successfully processed.
        /// </summary>
        public static MessageProcessingResult Success(string messageId) => new(messageId);

        /// <summary>
        /// Creates an <see cref="MessageProcessingResult"/> instance that represents a result of a message that was unsuccessfully processed.
        /// </summary>
        public static MessageProcessingResult Failure(string messageId, MessageProcessingError error, string errorMessage)
        {
            return new MessageProcessingResult(messageId, error, errorMessage, processingException: null);
        }

        /// <summary>
        /// Creates an <see cref="MessageProcessingResult"/> instance that represents a result of a message that was unsuccessfully processed.
        /// </summary>
        public static MessageProcessingResult Failure(string messageId, MessageProcessingError error, string errorMessage, Exception processingException)
        {
            return new MessageProcessingResult(
                messageId,
                error,
                errorMessage,
                processingException ?? throw new ArgumentNullException(nameof(processingException)));
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return IsSuccessful ? $"[Success]: {MessageId}" : $"[Failure]: {Error} {ErrorMessage}";
        }
    }
}
