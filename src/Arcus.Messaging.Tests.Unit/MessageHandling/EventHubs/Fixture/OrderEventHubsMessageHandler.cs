using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
#if NET6_0
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling; 
#endif
using Arcus.Messaging.Tests.Core.Messages.v1;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Fixture
{
#if NET6_0
    public class OrderEventHubsMessageHandler : IAzureEventHubsMessageHandler<Order>
    {
        public bool IsProcessed { get; private set; }

        public Task ProcessMessageAsync(
            Order message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IsProcessed = true;
            return Task.CompletedTask;
        }
    }
#endif
}
