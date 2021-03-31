using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test version of the <see cref="IFallbackMessageHandler"/> to have a solid implementation.
    /// </summary>
    public class PassThruFallbackMessageHandler : IFallbackMessageHandler
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
        public Task ProcessMessageAsync(
            string message,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
