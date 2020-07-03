using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation with the <see cref="TestMessage"/> and a specific <see cref="TestMessageContext"/>.
    /// </summary>
    public class TestMessageHandler : IMessageHandler<TestMessage, TestMessageContext>
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
            TestMessage message,
            TestMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IsProcessed = true;
            return Task.CompletedTask;
        }
    }
}
