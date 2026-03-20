using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;

namespace Arcus.Messaging.Tests.Core.ServiceBus.MessageHandlers
{
    /// <summary>
    /// Represents an 'intermediary' message handler that is used to assert on internal infrastructure correctness,
    /// without affecting the regular test behavior of the other message handlers in test scenarios.
    /// </summary>
    /// <remarks>
    ///     This message handler will always fail to prevent regular test behavior,
    /// </remarks>
    public class IntermediaryServiceBusMessageHandler : IServiceBusMessageHandler<Order>
    {
        private readonly Action<ServiceBusMessageContext> _assertContext;

        public IntermediaryServiceBusMessageHandler(Action<ServiceBusMessageContext> assertContext)
        {
            ArgumentNullException.ThrowIfNull(assertContext);
            _assertContext = assertContext;
        }

        public Task ProcessMessageAsync(
            Order message,
            ServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _assertContext(messageContext);
            throw new InvalidOperationException("[Test] always fail the special infrastructure assertion message handler to prevent regular test behavior");
        }
    }
}
