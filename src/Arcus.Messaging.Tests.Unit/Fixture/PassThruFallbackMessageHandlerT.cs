using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test version of the <see cref="IFallbackMessageHandler"/> to have a solid implementation.
    /// </summary>
    public class PassThruFallbackMessageHandler<TMessageContext> : IFallbackMessageHandler<string, TMessageContext> 
        where TMessageContext : MessageContext
    {
        /// <summary>
        /// Gets the flag indicating whether the fallback handler has processed the message.
        /// </summary>
        public bool IsProcessed { get; private set; }

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
        public Task ProcessMessageAsync(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IsProcessed = true;
            return Task.CompletedTask;
        }
    }
}
