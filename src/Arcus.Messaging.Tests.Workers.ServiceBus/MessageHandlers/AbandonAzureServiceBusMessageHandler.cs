using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class AbandonAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        public async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await messageContext.AbandonMessageAsync(messageContext.Properties, cancellationToken);
        }
    }
}