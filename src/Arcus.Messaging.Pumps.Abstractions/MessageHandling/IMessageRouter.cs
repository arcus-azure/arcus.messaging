using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents how incoming messages can be routed through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.
    /// </summary>
    /// <typeparam name="TMessageContext">The type of the message context within the incoming messages are processed.</typeparam>
    public interface IMessageRouter<in TMessageContext> where TMessageContext : MessageContext
    {
        /// <summary>
        ///     Handle a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>

        Task ProcessMessageAsync(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents how incoming messages can be routed through registered <see cref="IMessageHandler{TMessage}"/> instances.
    /// </summary>
    public interface IMessageRouter : IMessageRouter<MessageContext>
    {
    }
}