﻿using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class AbandonAzureServiceBusMessageHandler : AzureServiceBusMessageHandler<Order>
    {
        public AbandonAzureServiceBusMessageHandler (ILogger<AbandonAzureServiceBusMessageHandler > logger) : base(logger)
        {
        }

        public override async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await AbandonMessageAsync();
        }
    }
}