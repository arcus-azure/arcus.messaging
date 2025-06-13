using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.Extensions
{
    public partial class MessageHandlerCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithImplementationFactory_UsesFilter(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedHandler = new TestMessageHandler();
            var expectedContext = TestMessageContext.Generate();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: context =>
            {
                Assert.Same(expectedContext, context);
                return matches;
            }, implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true, implementationFactory: null));
        }
    }
}
