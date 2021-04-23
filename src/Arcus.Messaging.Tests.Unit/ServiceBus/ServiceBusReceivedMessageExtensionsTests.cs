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
