using System;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Fallback version of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> to have a safety net when no message handlers are able to process the message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message that this fallback message handler can handle.</typeparam>
    /// <typeparam name="TMessageContext">The type of the context in which the message is being processed.</typeparam>
    public interface IFallbackMessageHandler<in TMessage, in TMessageContext>
        where TMessage : class
        where TMessageContext : MessageContext
    {
        /// <summary>
        /// Process a <paramref name="message"/> that was received but couldn't be handled by any of the general registered message handlers.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, the <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        Task ProcessMessageAsync(
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Fallback version of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> to have a safety net when no message handlers able to process the message.
    /// </summary>
    public interface IFallbackMessageHandler<in TMessage> : IMessageHandler<TMessage, MessageContext>
    {
    }

    /// <summary>
    /// Fallback version of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> to have a safety net when no message handlers able to process the message.
    /// </summary>
    public interface IFallbackMessageHandler : IFallbackMessageHandler<string, MessageContext>
    {
    }
}
