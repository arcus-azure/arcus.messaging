using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test <see cref="IMessageHandler{TMessage}"/> implementation to stub out any generic messages and contexts.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to handle.</typeparam>
    /// <typeparam name="TMessageContext">The type of the context to handle.</typeparam>
    public class StubTestMessageHandler<TMessage, TMessageContext> : IMessageHandler<TMessage, TMessageContext> where TMessageContext : MessageContext 
    {
        /// <summary>
        /// Gets the flag indicating whether the handler has processed the message.
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
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IsProcessed = true;
            return Task.CompletedTask;
        }
    }
}
