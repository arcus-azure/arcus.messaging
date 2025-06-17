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
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithMessageBodyFilterWithImplementationFactory_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                },
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilterWithMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithoutMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithMessageBodyFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }
    }
}
