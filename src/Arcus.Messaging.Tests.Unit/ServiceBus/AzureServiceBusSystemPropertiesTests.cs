using System;
using Arcus.Messaging.Abstractions.ServiceBus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class AzureServiceBusSystemPropertiesTests
    {
        [Fact]
        public void CreateProperties_WithoutServiceBusMessage_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => AzureServiceBusSystemProperties.CreateFrom(message: null));
        }
    }
}
