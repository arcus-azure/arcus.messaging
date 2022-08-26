using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Fixture
{
    public class TestEventHubsMessageHandler : IAzureEventHubsMessageHandler<TestMessage>
    {
        public Task ProcessMessageAsync(
            TestMessage message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
