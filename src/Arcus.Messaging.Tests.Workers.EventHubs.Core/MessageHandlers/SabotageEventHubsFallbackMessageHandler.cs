using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.EventHubs;

namespace Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers
{
    public class SabotageEventHubsFallbackMessageHandler : IFallbackMessageHandler<string, AzureEventHubsMessageContext>
    {
        public Task ProcessMessageAsync(
            string message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Sabotage this!");
        }
    }
}
