using System;
#if NET6_0
using Arcus.Messaging.Pumps.EventHubs.Configuration; 
#endif
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs
{
#if NET6_0
    public class AzureEventHubsMessagePumpOptionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void SetConsumerGroup_WithoutValue_Fails(string consumerGroup)
        {
            // Arrange
            var options = new AzureEventHubsMessagePumpOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.ConsumerGroup = consumerGroup);
        }
    }
#endif
}
