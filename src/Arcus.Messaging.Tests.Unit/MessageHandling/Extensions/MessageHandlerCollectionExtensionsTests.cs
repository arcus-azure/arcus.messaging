using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.Extensions
{
    public partial class MessageHandlerCollectionExtensionsTests
    {
        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(implementationFactory: null));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(implementationFactory: null));
        }

        [Fact]
        public void WithFallbackMessageHandler_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>(createImplementation: null));
        }
    }
}
