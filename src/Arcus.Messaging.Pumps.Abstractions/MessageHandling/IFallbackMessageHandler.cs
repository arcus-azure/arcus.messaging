using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Fallback version of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> to have a safety net when no handlers are found could process the message.
    /// </summary>
    public interface IFallbackMessageHandler
    {
        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">
        ///     The information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to cancel the processing.</param>
        Task ProcessMessageAsync(
            string message,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }
}
