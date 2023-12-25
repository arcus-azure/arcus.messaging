using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
#if NET6_0
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling; 
#endif

namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Fixture
{
#if NET6_0
    public class IgnoreEventHubsMessageHandler<TMessage> : IAzureEventHubsMessageHandler<TMessage>
    {
        public bool IsProcessed { get; private set; }
        public Task ProcessMessageAsync(
            TMessage message,
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
