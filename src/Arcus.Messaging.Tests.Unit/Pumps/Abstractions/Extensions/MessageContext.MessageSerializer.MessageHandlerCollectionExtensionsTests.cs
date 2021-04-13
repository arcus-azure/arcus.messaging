using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ArgumentException = System.ArgumentException;

namespace Arcus.Messaging.Tests.Unit.Pumps.Abstractions.Extensions
{
    public partial class MessageHandlerCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageContextFilterMessageBodySerializer_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializer: serializer);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithMessageBodySerializer_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithoutMessageBodySerializer_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageContextFilterMessageBodySerializerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithoutMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageContextFilterMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(expectedHandler.GetType(), handler.GetMessageHandlerType());
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null,
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithMessageBodySerializerWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializer: serializer,
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageContextFilterWithMessageBodySerializerWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithoutMessageBodySerializerWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializer: null,
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageContextFilterWithMessageBodySerializerWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageContextFilterMessageBodySerializer_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializer: serializer);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilterWithMessageBodySerializer_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithoutMessageBodySerializer_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageContextFilterMessageBodySerializerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithoutMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageContextFilterMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(expectedHandler.GetType(), handler.GetMessageHandlerType());
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageContextFilterWithMessageBodySerializerWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializer: serializer,
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            bool actual = handler.CanProcessMessageBasedOnContext(expectedContext);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageContextFilterWithMessageBodySerializerWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithoutMessageBodySerializerWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializer: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageContextFilterWithMessageBodySerializerWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: null));
        }
    }
}
