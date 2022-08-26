using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class TestEventHubsMessageHandler<TMessage> : IAzureEventHubsMessageHandler<TMessage>
    {
        public Task ProcessMessageAsync(
            TMessage message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
