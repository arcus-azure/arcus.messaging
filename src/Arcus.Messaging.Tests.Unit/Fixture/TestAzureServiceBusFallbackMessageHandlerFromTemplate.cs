using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    public class TestAzureServiceBusFallbackMessageHandlerFromTemplate : AzureServiceBusFallbackMessageHandler
    {
        public TestAzureServiceBusFallbackMessageHandlerFromTemplate(
            ILogger<TestAzureServiceBusFallbackMessageHandlerFromTemplate> logger) 
            : base(logger)
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
