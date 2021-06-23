using System;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Extensions
{
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void WithServiceBusMessageRouting_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddServiceBusMessageRouting<TestAzureServiceBusMessageRouter>(implementationFactory: null));
        }
    }
}
