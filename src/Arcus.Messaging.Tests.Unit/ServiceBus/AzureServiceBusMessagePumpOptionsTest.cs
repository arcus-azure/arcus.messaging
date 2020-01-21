using System;
using Arcus.Messaging.Pumps.ServiceBus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class AzureServiceBusMessagePumpOptionsTest
    {
        [Fact]
        public void MaxConcurrentCalls_ValueIsAboveZero_Succeeds()
        {
            // Arrange
            var validConcurrentCalls = 1337;

            // Act
            var messagePumpOptions = new AzureServiceBusMessagePumpOptions {MaxConcurrentCalls = validConcurrentCalls};

            // Assert
            Assert.Equal(validConcurrentCalls, messagePumpOptions.MaxConcurrentCalls);
        }

        [Fact]
        public void MaxConcurrentCalls_ValueIsZero_ThrowsException()
        {
            // Arrange
            var invalidConcurrentCalls = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new AzureServiceBusMessagePumpOptions
                {MaxConcurrentCalls = invalidConcurrentCalls});
        }

        [Fact]
        public void MaxConcurrentCalls_ValueIsNegative_ThrowsException()
        {
            // Arrange
            var invalidConcurrentCalls = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new AzureServiceBusMessagePumpOptions
                {MaxConcurrentCalls = invalidConcurrentCalls});
        }
    }
}