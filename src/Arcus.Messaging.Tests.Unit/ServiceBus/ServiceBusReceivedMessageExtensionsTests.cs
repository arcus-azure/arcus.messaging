using System;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.ServiceBus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class ServiceBusReceivedMessageExtensionsTests
    {
        [Fact]
        public void CreateMessage_GetMessageContext_Succeeds()
        {
            // Arrange
            Order order = OrderGenerator.Generate();
            string messageId = Guid.NewGuid().ToString();
            ServiceBusReceivedMessage message =
                ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: messageId,
                    body: BinaryData.FromObjectAsJson(order));
            string jobId = Guid.NewGuid().ToString();

            // Act
            AzureServiceBusMessageContext context = message.GetMessageContext(jobId);

            // Assert
            Assert.Equal(jobId, context.JobId);
            Assert.Equal(messageId, context.MessageId);
            Assert.NotNull(context.SystemProperties);
        }

        [Fact]
        public void CreateMessage_GetSystemProperties_Succeeds()
        {
            // Arrange
            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            
            // Act
            AzureServiceBusSystemProperties systemProperties = message.GetSystemProperties();
            
            // Assert
            Assert.NotNull(systemProperties);
        }
    }
}
