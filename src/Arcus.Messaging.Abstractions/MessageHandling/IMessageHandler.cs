using System.Threading;
using System.Threading.Tasks;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a handler for a specific <typeparamref name="TMessage"/> in a <typeparamref name="TMessageContext"/>
    /// during the processing of the message pump or router.
    /// </summary>
    public interface IMessageHandler<in TMessage, in TMessageContext> where TMessageContext : MessageContext
    {
        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessMessageAsync(
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents a handler for a specific <typeparamref name="TMessage"/> in a <see cref="MessageContext"/>
    /// during the processing of the message pump or router.
    /// </summary>
    public interface IMessageHandler<in TMessage> : IMessageHandler<TMessage, MessageContext>
    {
    }
}
