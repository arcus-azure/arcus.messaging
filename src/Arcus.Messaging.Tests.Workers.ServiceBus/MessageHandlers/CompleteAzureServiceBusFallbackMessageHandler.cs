using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class CompleteAzureServiceBusFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
    {
        public CompleteAzureServiceBusFallbackMessageHandler(
            ILogger<WriteOrderToDiskFallbackMessageHandler> logger) : base(logger)
        {
        }

        public override async Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await CompleteMessageAsync(message);
        }
    }
}