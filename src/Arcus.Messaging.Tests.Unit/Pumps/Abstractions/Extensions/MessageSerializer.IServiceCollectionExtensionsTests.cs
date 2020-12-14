using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.Abstractions.Extensions
{
    public  class MessageSerializerIServiceCollectionExtensionsTests
    {
        [Fact]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageBodySerializer_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageBodySerializer: serializer);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageBodySerializer_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageBodySerializer: null));
        }

        [Fact]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializer_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageBodySerializer: serializer);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializer_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageBodySerializer: null));
        }

        [Fact]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageBodySerializerWithImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageBodySerializer: serializer, implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageBodySerializerWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageBodySerializer: null, implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageBodySerializerWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageBodySerializer: Mock.Of<IMessageBodySerializer>(), implementationFactory: null));
        }

        [Fact]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializerWithImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodySerializer: serializer, implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializerWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializer: null, implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializer: Mock.Of<IMessageBodySerializer>(), implementationFactory: null));
        }

        [Fact]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageBodySerializerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: null));
        }

        [Fact]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: null));
        }

        [Fact]
        public async Task WithMessageHandlerWithDefaultContext_WithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer, 
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithoutMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: null,
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithDefaultContext_WithMessageBodySerializerImplementationFactoryWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: null));
        }

        [Fact]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer, 
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactoryWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: null));
        }
    }
}
