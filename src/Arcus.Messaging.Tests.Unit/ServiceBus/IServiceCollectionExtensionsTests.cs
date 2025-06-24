using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void WithServiceBusMessageHandler_WithoutImplementationFactory_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: null));
        }
    }
}
