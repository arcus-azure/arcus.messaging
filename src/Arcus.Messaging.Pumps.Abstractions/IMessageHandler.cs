using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    /// Represents a handler for a specific <typeparamref name="TMessage"/> in a <typeparamref name="TMessageContext"/>
    /// during the processing of the <see cref="MessagePump{TMessage,TMessageContext}"/>.
    /// </summary>
    public interface IMessageHandler<in TMessage, in TMessageContext> where TMessageContext : MessageContext
    {
        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry & processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <typeparam name="TMessage">Type of message we are interested in.</typeparam>
        /// <typeparam name="TMessageContext">Type of message context for the provider.</typeparam>
        Task ProcessMessageAsync(
            TMessage message, 
            TMessageContext messageContext, 
            MessageCorrelationInfo correlationInfo, 
            CancellationToken cancellationToken);
    }
}
