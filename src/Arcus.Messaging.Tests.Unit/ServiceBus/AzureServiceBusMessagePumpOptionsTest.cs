using System;
using System.Collections.Generic;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class AzureServiceBusMessagePumpOptionsTest
    {
        public static IEnumerable<object[]> CreateOptionsWithMaxConcurrentCalls => new[]
        {
            new object[] { new Func<int, AzureServiceBusMessagePumpOptions>(max => new AzureServiceBusQueueMessagePumpOptions { MaxConcurrentCalls = max }) },
            new object[] { new Func<int, AzureServiceBusMessagePumpOptions>(max => new AzureServiceBusTopicMessagePumpOptions { MaxConcurrentCalls = max }) }
        };

        [Theory]
        [MemberData(nameof(CreateOptionsWithMaxConcurrentCalls))]
        public void TopicOptionsMaxConcurrentCalls_ValueIsAboveZero_Succeeds(Func<int, AzureServiceBusMessagePumpOptions> createOptions)
        {
            // Arrange
            var validConcurrentCalls = 1337;

            // Act
            AzureServiceBusMessagePumpOptions messagePumpOptions = createOptions(validConcurrentCalls);

            // Assert
            Assert.Equal(validConcurrentCalls, messagePumpOptions.MaxConcurrentCalls);
        }

        [Theory]
        [MemberData(nameof(CreateOptionsWithMaxConcurrentCalls))]
        public void TopicOptionsMaxConcurrentCalls_ValueIsZero_ThrowsException(Func<int, AzureServiceBusMessagePumpOptions> createOptions)
        {
            // Arrange
            var invalidConcurrentCalls = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => createOptions(invalidConcurrentCalls));
        }

        [Theory]
        [MemberData(nameof(CreateOptionsWithMaxConcurrentCalls))]
        public void TopicOptionsMaxConcurrentCalls_ValueIsNegative_ThrowsException(Func<int, AzureServiceBusMessagePumpOptions> createOptions)
        {
            // Arrange
            var invalidConcurrentCalls = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => createOptions(invalidConcurrentCalls));
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