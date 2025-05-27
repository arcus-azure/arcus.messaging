using System;
using Arcus.Messaging.Abstractions.ServiceBus;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.Logging;
using Moq;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Stubs
{
    /// <summary>
    /// Test factory to generate valid <see cref="AzureServiceBusMessageContext"/> instances.
    /// </summary>
    public static class AzureServiceBusMessageContextFactory
    {
        private static readonly Faker Bogus = new Faker();

        /// <summary>
        /// Generates a valid <see cref="AzureServiceBusMessageContext"/> instance.
        /// </summary>
        public static AzureServiceBusMessageContext Generate()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                messageId: $"message-id-{Guid.NewGuid()}",
                deliveryCount: Bogus.Random.Int());

            var context = AzureServiceBusMessageContext.Create(
                $"job-id-{Guid.NewGuid()}",
                Bogus.PickRandom<ServiceBusEntityType>(),
                Mock.Of<ServiceBusReceiver>(),
                message);

            return context;
        }
    }
}
