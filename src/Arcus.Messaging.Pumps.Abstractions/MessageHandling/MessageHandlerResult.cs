using System;
using GuardNet;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Result data type to represent the outcome of the message handling processing in the <see cref="MessagePump.ProcessMessageAndCaptureAsync{TMessageContext}"/>.
    /// </summary>
    public class MessageHandlerResult
    {
        private MessageHandlerResult(bool isProcessed, Exception exception)
        {
            Guard.For<ArgumentException>(
                () => !isProcessed && exception is null, 
                "Requires an exception when the message handler result is a failure");

            IsProcessed = isProcessed;
            Exception = exception;
        }

        /// <summary>
        /// Gets the flag indicating whether the message pump was able to process the message or not.
        /// </summary>
        public bool IsProcessed { get; }

        /// <summary>
        /// Gets the (optional) exception that was thrown during the message processing.
        /// Only available when their was an exception thrown.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Creates an <see cref="MessageHandlerResult"/> instance that means that an <see cref="IMessageHandler{TMessage,TMessageContext}"/> was able to process the message.
        /// </summary>
        public static MessageHandlerResult Success()
        {
            return new MessageHandlerResult(isProcessed: true, exception: null);
        }

        /// <summary>
        /// Creates an <see cref="MessageHandlerResult"/> instance that means that no <see cref="IMessageHandler{TMessage,TMessageContext}"/> was found to process the message.
        /// A custom fallback mechanism could happen in this case.
        /// </summary>
        public static MessageHandlerResult Pending()
        {
            return new MessageHandlerResult(isProcessed: false, exception: null);
        }

        /// <summary>
        /// Creates an <see cref="MessageHandlerResult"/> instance that means that an exception was thrown during the message processing either in an specific <see cref="IMessageHandler{TMessage,TMessageContext}"/>
        /// or in the message processing logic itself.
        /// </summary>
        /// <param name="exception">The exception that was thrown.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> is <c>null</c>.</exception>
        public static MessageHandlerResult Failure(Exception exception)
        {
            Guard.NotNull(exception, nameof(exception), "Requires an exception when the message handler result is a failure");
            return new MessageHandlerResult(isProcessed: false, exception: exception);
        }
    }
}
