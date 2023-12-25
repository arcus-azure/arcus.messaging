using System;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Bogus;
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
            var invalidConcurrentCalls = new Faker().Random.Number(min: -9999, max: -1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => options.MaxConcurrentCalls = invalidConcurrentCalls);
        }

        [Fact]
        public void TopicOptionsPrefetchCount_ValueIsAboveZero_Succeeds()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var validPrefetchCount = new Faker().Random.Number(min: 1, max: 500);

            // Act
            options.PrefetchCount = validPrefetchCount;

            // Assert
            Assert.Equal(validPrefetchCount, options.PrefetchCount);
        }

        [Fact]
        public void TopicOptionsPrefetchCount_ValueIsZero_Succeeds()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var validPrefetchCount = 0;

            // Act
            options.PrefetchCount = validPrefetchCount;

            // Assert
            Assert.Equal(validPrefetchCount, options.PrefetchCount);
        }

        [Fact]
        public void TopicOptionsPrefetchCount_ValueIsNegative_ThrowsException()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var invalidPrefetchCount = new Faker().Random.Number(min: -9999, max: -1); 

            // Act & Assert
            Assert.Throws<ArgumentException>(() => options.PrefetchCount = invalidPrefetchCount);
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

        [Theory]
        [ClassData(typeof(Blanks))]
        public void OperationParentIdPropertyName_ValueIsBlank_Throws(string operationParentIdPropertyName)
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                options.Correlation.OperationParentIdPropertyName = operationParentIdPropertyName);
        }

        [Fact]
        public void OperationParentIdPropertyName_ValueNotBlank_Succeeds()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            // Act
            options.Correlation.OperationParentIdPropertyName = operationParentId;

            // Assert
            Assert.Equal(operationParentId, options.Correlation.OperationParentIdPropertyName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void OperationName_ValueIsBlank_Throws(string operationName)
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                options.Routing.Telemetry.OperationName = operationName);
        }

        [Fact]
        public void OperationName_ValueNotBlank_Succeeds()
        {
            // Arrange
            var options = new AzureServiceBusMessagePumpOptions();
            var operationName = $"operation-name-{Guid.NewGuid()}";

            // Act
            options.Routing.Telemetry.OperationName = operationName;

            // Assert
            Assert.Equal(operationName, options.Routing.Telemetry.OperationName);
        }

    }
}