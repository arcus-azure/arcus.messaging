using System;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus;
using Bogus;
using Moq;
using ServiceBusEntityType = Arcus.Messaging.Abstractions.ServiceBus.ServiceBusEntityType;

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
        public static AzureServiceBusMessageContext Generate(string jobId = null)
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                messageId: $"message-id-{Guid.NewGuid()}",
                deliveryCount: Bogus.Random.Int());

            var context = ServiceBusMessageContext.Create(
                Mock.Of<ServiceBusReceiver>(),
                message,
                options =>
                {
                    options.JobId = jobId ?? $"job-id-{Guid.NewGuid()}";
                    options.SubscriptionName = Bogus.PickRandom<ServiceBusEntityType>() switch
                    {
                        ServiceBusEntityType.Queue => null,
                        ServiceBusEntityType.Topic => $"subscription-{Guid.NewGuid()}",
                        _ => throw new ArgumentOutOfRangeException(nameof(options), "Invalid Azure Service Bus entity type")
                    };
                });

            return new Mock<AzureServiceBusMessageContext>(context).Object;
        }
    }
}
