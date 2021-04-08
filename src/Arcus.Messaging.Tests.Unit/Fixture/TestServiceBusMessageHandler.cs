using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test <see cref="IAzureServiceBusMessageHandler{TMessage}"/> implementation processing the <see cref="TestMessage"/>.
    /// </summary>
    public class TestServiceBusMessageHandler : IAzureServiceBusMessageHandler<TestMessage>
    {
        /// <summary>
        /// Gets the flag indicating whether or not this message handler was being used to process a message.
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
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IsProcessed = true;
            return Task.CompletedTask;
        }
    }
}
