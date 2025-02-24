using System.Threading;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.ServiceBus;
using Moq;
using Xunit;

#pragma warning disable CS0618 // Disable warning for usage of deprecated functionality that will be removed in v3.0.

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class ProcessMessageEventArgsExtensionsTests
    {
        [Fact]
        public void CreateEventArgs_WithServiceBusReceiver_GetsServiceBusReceiverSucceeds()
        {
            // Arrange
            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            var expectedReceiver = Mock.Of<ServiceBusReceiver>();

            // Act
            var eventArgs = new ProcessMessageEventArgs(message, expectedReceiver, CancellationToken.None);

            // Assert
            ServiceBusReceiver actualReceiver = eventArgs.GetServiceBusReceiver();
            Assert.Equal(expectedReceiver, actualReceiver);
        }
    }
}
