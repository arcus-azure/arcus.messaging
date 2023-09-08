using System;
using Arcus.Messaging.Abstractions.ServiceBus;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus
{
    public class AzureServiceBusMessageContextTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void Create_WithoutEntityType_IsUnknown()
        {
            // Arrange
            ServiceBusReceivedMessage message = CreateReceivedMessage();
            var jobId = Bogus.Random.Guid().ToString();

            // Act
            AzureServiceBusMessageContext context = message.GetMessageContext(jobId);

            // Assert
            Assert.Equal(jobId, context.JobId);
            Assert.Equal(message.MessageId, context.MessageId);
            Assert.Equal(ServiceBusEntityType.Unknown, context.EntityType);
        }

        [Fact]
        public void Create_WithEntityType_IsKnown()
        {
            // Arrange
            ServiceBusReceivedMessage message = CreateReceivedMessage();
            var jobId = Bogus.Random.Guid().ToString();
            var entityType = Bogus.PickRandomWithout(ServiceBusEntityType.Unknown);

            // Act
            AzureServiceBusMessageContext context = message.GetMessageContext(jobId, entityType);

            // Assert
            Assert.Equal(jobId, context.JobId);
            Assert.Equal(message.MessageId, context.MessageId);
            Assert.Equal(entityType, context.EntityType);
        }

        private static ServiceBusReceivedMessage CreateReceivedMessage()
        {
            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromBytes(Bogus.Random.Bytes(100)),
                messageId: Bogus.Random.Guid().ToString());
        }
    }
}
