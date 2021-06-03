using System;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class AzureServiceBusMessagePumpOptionsTest
    {
        [Fact]
        public void TopicOptionsMaxConcurrentCalls_ValueIsAboveZero_Succeeds()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var validConcurrentCalls = 1337;

            // Act
            options.MaxConcurrentCalls = validConcurrentCalls;

            // Assert
            Assert.Equal(validConcurrentCalls, options.MaxConcurrentCalls);
        }

        [Fact]
        public void TopicOptionsMaxConcurrentCalls_ValueIsZero_ThrowsException()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var invalidConcurrentCalls = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => options.MaxConcurrentCalls = invalidConcurrentCalls);
        }

        [Fact]
        public void TopicOptionsMaxConcurrentCalls_ValueIsNegative_ThrowsException()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var invalidConcurrentCalls = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => options.MaxConcurrentCalls = invalidConcurrentCalls);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void TransactionIdPropertyName_ValueIsBlank_Throws(string transactionIdPropertyName)
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.Correlation.TransactionIdPropertyName = transactionIdPropertyName);
        }

        [Fact]
        public void TransactionIdPropertyName_ValueNotBlank_Succeeds()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            const string expected = "Transaction-ID";
            
            // Act
            options.Correlation.TransactionIdPropertyName = expected;
            
            // Assert
            Assert.Equal(expected, options.Correlation.TransactionIdPropertyName);
        }
    }
}