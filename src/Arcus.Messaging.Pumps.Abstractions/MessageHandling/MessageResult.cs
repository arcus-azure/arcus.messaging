using System;
using GuardNet;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a type that's the result of a successful or faulted message deserialization of an <see cref="IMessageBodySerializer"/> instance.
    /// </summary>
    /// <seealso cref="IMessageBodySerializer"/>
    public class MessageResult
    {
        private MessageResult(object result)
        {
            IsSuccess = true;
            DeserializedMessage = result;
        }

        private MessageResult(Exception exception)
        {
            IsSuccess = false;
            Exception = exception;
            ErrorMessage = exception.Message;
        }

        private MessageResult(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets a flag indicating whether or not the message was successfully deserialized.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the deserialized message instance after the <see cref="IMessageBodySerializer"/>.
        /// </summary>
        /// <remarks>
        ///     Only contains a value if the deserialization in the <see cref="IMessageBodySerializer"/> was successful (<see cref="IsSuccess"/> is <c>true</c>).
        /// </remarks>
        public object DeserializedMessage { get; }

        /// <summary>
        /// Gets the optional error message describing the failure during the deserialization in the <see cref="IMessageBodySerializer"/>.
        /// </summary>
        /// <remarks>
        ///     Only contains value if the deserialization in the <see cref="IMessageBodySerializer"/> was faulted (<see cref="IsSuccess"/> is <c>false</c>).
        /// </remarks>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the optional exception thrown during the deserialization in the <see cref="IMessageBodySerializer"/>.
        /// </summary>
        /// <remarks>
        ///     Only contains a value if the deserialization in the <see cref="IMessageBodySerializer"/> was faulted (<see cref="IsSuccess"/> is <c>false</c>)
        ///     and their was an exception thrown during the deserialization.
        /// </remarks>
        public Exception Exception { get; }

        /// <summary>
        /// Creates a <see cref="MessageResult"/> that represents a successful deserialization.
        /// </summary>
        /// <param name="message">The deserialized message instance.</param>
        public static MessageResult Success(object message)
        {
            Guard.NotNull(message, nameof(message), "Requires a deserialized message instance when the message deserialization was successful");
            return new MessageResult(message);
        }

        /// <summary>
        /// Creates a <see cref="MessageResult"/> that represents a faulted deserialization.
        /// </summary>
        /// <param name="errorMessage">The message describing the deserialization error.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="errorMessage"/> is blank.</exception>
        public static MessageResult Failure(string errorMessage)
        {
            Guard.NotNullOrWhitespace(errorMessage, nameof(errorMessage), "Requires a non-blank error message describing the deserialization failure");
            return new MessageResult(errorMessage);
        }

        /// <summary>
        /// Creates a <see cref="MessageResult"/> that represents a faulted deserialization.
        /// </summary>
        /// <param name="exception">The exception describing the deserialization failure.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> is <c>null</c>.</exception>
        public static MessageResult Failure(Exception exception)
        {
            Guard.NotNull(exception, nameof(exception), "Requires an exception describing the deserialization failure");
            return new MessageResult(exception);
        }
    }
}