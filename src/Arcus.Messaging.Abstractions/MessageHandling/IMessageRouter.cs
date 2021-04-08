using System;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents how incoming messages can be routed through registered <see cref="IMessageHandler{TMessage}"/> instances.
    /// </summary>
    public interface IMessageRouter
    {
        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        Task RouteMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext;
    }
}