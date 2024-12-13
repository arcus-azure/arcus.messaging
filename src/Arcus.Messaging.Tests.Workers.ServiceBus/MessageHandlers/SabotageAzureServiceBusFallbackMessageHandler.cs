using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.ServiceBus;
using InvalidOperationException = System.InvalidOperationException;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers
{
    public class SabotageAzureServiceBusFallbackMessageHandler : 
        IAzureServiceBusFallbackMessageHandler, 
        IFallbackMessageHandler,
        IFallbackMessageHandler<Order, AzureServiceBusMessageContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SabotageAzureServiceBusFallbackMessageHandler" /> class.
        /// </summary>
        public SabotageAzureServiceBusFallbackMessageHandler()
        {
            
        }

        public Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Sabotage fallback!");
        }

        public Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Sabotage fallback!");
        }

        public Task ProcessMessageAsync(
            string message,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Sabotage fallback!");
        }
    }
}
