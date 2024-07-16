using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class AbandonAzureServiceBusFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
    {
        public AbandonAzureServiceBusFallbackMessageHandler(ILogger<AbandonAzureServiceBusFallbackMessageHandler> logger) : base(logger)
        {
        }

        public override async Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await AbandonMessageAsync(message);
        }
    }
}