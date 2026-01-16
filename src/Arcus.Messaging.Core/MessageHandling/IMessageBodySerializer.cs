using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a handler that provides a deserialization strategy for the incoming message during the message processing of message pump or router.
    /// </summary>
    /// <seealso cref="IMessageHandler{TMessage,TMessageContext}"/>
    [Obsolete("Will be removed in v3.0 in favor of using the new " + nameof(IMessageBodyDeserializer) + " interface", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
    public interface IMessageBodySerializer
    {
        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        Task<MessageResult> DeserializeMessageAsync(string messageBody);
    }
}

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a handler that provides a deserialization strategy for the incoming message during the message processing of message pump or router.
    /// </summary>
    /// <seealso cref="IMessageHandler{TMessage,TMessageContext}"/>
    public interface IMessageBodyDeserializer
    {
        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody);
    }

    /// <summary>
    /// Represents a type that's the result of a successful or faulted message deserialization of an <see cref="IMessageBodyDeserializer"/> instance.
    /// </summary>
    public class MessageBodyResult
    {
        private readonly object _deserializedBody;
        private readonly string _failureMessage;
        private readonly Exception _failureCause;

        private MessageBodyResult(object deserializedBody)
        {
            ArgumentNullException.ThrowIfNull(deserializedBody);
            _deserializedBody = deserializedBody;

            IsSuccess = true;
        }

        private MessageBodyResult(string failureMessage, Exception failureCause = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

            _failureMessage = failureMessage;
            _failureCause = failureCause;

            IsSuccess = false;
        }

        /// <summary>
        /// Gets the boolean flag indicating whether or not the message was successfully deserialized.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the deserialized message body represented as a concrete object instance.
        /// </summary>
        /// <remarks>
        ///     Only available when the deserialization was successful (<see cref="IsSuccess"/> is <c>true</c>).
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when this instance does not represent a successful result.</exception>
        public object DeserializedBody => IsSuccess ? _deserializedBody : throw new InvalidOperationException("[Arcus] the message body result does not represent a successful result, therefore no deserialized message body object instance is available");

        /// <summary>
        /// Gets the message describing the failure during the deserialization.
        /// </summary>
        /// <remarks>
        ///     Only available when the deserialization was not successful (<see cref="IsSuccess"/> is <c>false</c>).
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when this instance does not represent a failed result.</exception>
        public string FailureMessage => !IsSuccess ? _failureMessage : throw new InvalidOperationException("[Arcus] the message body result represents a successful result, therefore no failure message is available");

        /// <summary>
        /// Gets the possibly occurred exception during the message body deserialization that caused the failure.
        /// </summary>
        /// <remarks>
        ///     Only available when the deserialization was not successful (<see cref="IsSuccess"/> is <c>false</c>)
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when this instance does not represent a failed result.</exception>
        public Exception FailureCause => !IsSuccess ? _failureCause : throw new InvalidOperationException("[Arcus] the message body result represents a successful result, therefore no failure cause is available");

        /// <summary>
        /// Creates a <see cref="MessageBodyResult"/> that represents a successful message body deserialization.
        /// </summary>
        /// <param name="deserializedBody">The deserialized message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="deserializedBody"/> is <c>null</c>.</exception>
        public static MessageBodyResult Success(object deserializedBody)
        {
            return new(deserializedBody);
        }

        /// <summary>
        /// Creates a <see cref="MessageBodyResult"/> that represents a faulted message body deserialization.
        /// </summary>
        /// <param name="failureMessage">The message describing the deserialization failure.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="failureMessage"/> is blank.</exception>
        public static MessageBodyResult Failure(string failureMessage)
        {
            return new(failureMessage);
        }

        /// <summary>
        /// Creates a <see cref="MessageBodyResult"/> that represents a faulted message body deserialization.
        /// </summary>
        /// <param name="failureMessage">The message describing the deserialization failure.</param>
        /// <param name="failureCause">The occurred exception that was the cause of the deserialization failure.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="failureMessage"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="failureCause"/> is <c>null</c>.</exception>
        public static MessageBodyResult Failure(string failureMessage, Exception failureCause)
        {
            ArgumentNullException.ThrowIfNull(failureCause);
            return new(failureMessage, failureCause);
        }
    }
}
