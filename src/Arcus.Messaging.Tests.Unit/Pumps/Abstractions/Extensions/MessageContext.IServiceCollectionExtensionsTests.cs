using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.Abstractions.Extensions
{
    public class MessageContextIServiceCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilter_UsesFilter(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageContextFilter: context =>
            {
                Assert.Same(expectedContext, context);
                return matches;
            });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageContextFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithImplementationFactory_UsesFilter(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedHandler = new DefaultTestMessageHandler();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageContextFilter: context =>
            {
                Assert.Same(expectedContext, context);
                return matches;
            }, implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null, implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false, implementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilter_UsesFilter(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedContext = TestMessageContext.Generate();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: context =>
            {
                Assert.Same(expectedContext, context);
                return matches;
            });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithImplementationFactory_UsesFilter(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedHandler = new TestMessageHandler();
            var expectedContext = TestMessageContext.Generate();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: context =>
            {
                Assert.Same(expectedContext, context);
                return matches;
            }, implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
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
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true, implementationFactory: null));
        }
    }
}
