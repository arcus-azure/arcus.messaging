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
            new object[] { new Func<int, AzureServiceBusMessagePumpOptionsBase>(max => new AzureServiceBusQueueMessagePumpOptions { MaxConcurrentCalls = max }) },
            new object[] { new Func<int, AzureServiceBusMessagePumpOptionsBase>(max => new AzureServiceBusTopicMessagePumpOptions { MaxConcurrentCalls = max }) }
        };

        [Theory]
        [MemberData(nameof(CreateOptionsWithMaxConcurrentCalls))]
        public void TopicOptionsMaxConcurrentCalls_ValueIsAboveZero_Succeeds(Func<int, AzureServiceBusMessagePumpOptionsBase> createOptions)
        {
            // Arrange
            var validConcurrentCalls = 1337;

            // Act
            AzureServiceBusMessagePumpOptionsBase messagePumpOptions = createOptions(validConcurrentCalls);

            // Assert
            Assert.Equal(validConcurrentCalls, messagePumpOptions.MaxConcurrentCalls);
        }

        [Theory]
        [MemberData(nameof(CreateOptionsWithMaxConcurrentCalls))]
        public void TopicOptionsMaxConcurrentCalls_ValueIsZero_ThrowsException(Func<int, AzureServiceBusMessagePumpOptionsBase> createOptions)
        {
            // Arrange
            var invalidConcurrentCalls = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => createOptions(invalidConcurrentCalls));
        }

        [Theory]
        [MemberData(nameof(CreateOptionsWithMaxConcurrentCalls))]
        public void TopicOptionsMaxConcurrentCalls_ValueIsNegative_ThrowsException(Func<int, AzureServiceBusMessagePumpOptionsBase> createOptions)
        {
            // Arrange
            var invalidConcurrentCalls = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => createOptions(invalidConcurrentCalls));
        }
    }
}