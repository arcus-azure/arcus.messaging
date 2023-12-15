using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    public class OrderV1AzureServiceBusMessageHandlerFromTemplate : AzureServiceBusMessageHandler<Core.Messages.v1.Order>
    {
        public OrderV1AzureServiceBusMessageHandlerFromTemplate(
            ILogger<OrderV1AzureServiceBusMessageHandlerFromTemplate> logger) 
            : base(logger)
        {
        }

        public bool IsConfigured { get; private set; }


        public override async Task ProcessMessageAsync(
            Core.Messages.v1.Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await CompleteMessageAsync();
        }
    }
}
