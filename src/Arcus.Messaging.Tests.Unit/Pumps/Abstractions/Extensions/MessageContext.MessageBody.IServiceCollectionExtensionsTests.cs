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
    public class MessageContextMessageBodyIServiceCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithMessageBodyFilter_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext($"message-id-{Guid.NewGuid()}", new Dictionary<string, object>());
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithMessageBodyFilterWithImplementationFactory_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext($"message-id-{Guid.NewGuid()}", new Dictionary<string, object>());
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
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
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithoutMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithMessageBodyFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithMessageBodyFilter_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var expectedHandler = new DefaultTestMessageHandler();

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
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithMessageBodyFilterWithImplementationFactory_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
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
            IServiceProvider provider = services.BuildServiceProvider();
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
            var services = new ServiceCollection();

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
            var services = new ServiceCollection();

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
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }
    }
}
