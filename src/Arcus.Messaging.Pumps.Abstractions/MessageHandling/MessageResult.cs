using System;
using GuardNet;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a type that's the result of a successful or faulted message deserialization of an <see cref="IMessageBodyHandler"/> instance.
    /// </summary>
    /// <seealso cref="IMessageBodyHandler"/>
    public class MessageResult
    {
        private MessageResult(bool isSuccess, object result)
        {
            Guard.For<ArgumentException>(
                () => !isSuccess && result != null,
                "Restricts an deserialized message instance only for successful message results");
            
            IsSuccess = isSuccess;
            DeserializedMessage = result;
        }

        /// <summary>
        /// Gets a flag indicating whether or not the message was successfully deserialized.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the deserialized message instance after the <see cref="IMessageBodyHandler"/>.
        /// </summary>
        /// <remarks>
        ///     Only contains a value if the deserialization in the <see cref="IMessageBodyHandler"/> was successful (<see cref="IsSuccess"/> is <c>true</c>).
        /// </remarks>
        public object DeserializedMessage { get; }

        /// <summary>
        /// Creates a <see cref="MessageResult"/> that represents a successful deserialization.
        /// </summary>
        /// <param name="message">The deserialized message instance.</param>
        public static MessageResult Success(object message)
        {
            Guard.NotNull(message, nameof(message), "Requires a deserialized message instance when the message deserialization was successful");
            return new MessageResult(isSuccess: true, result: message);
        }

        /// <summary>
        /// Creates a <see cref="MessageResult"/> that represents a faulted deserialization.
        /// </summary>
        public static MessageResult Failure()
        {
            return new MessageResult(isSuccess: false, result: null);
        }
    }
}