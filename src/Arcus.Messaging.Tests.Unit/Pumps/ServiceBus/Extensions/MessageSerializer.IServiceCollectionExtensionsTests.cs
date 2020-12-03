using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.ServiceBus.Extensions
{
    public class MessageSerializerIServiceCollectionExtensionsTests
    {
        [Fact]
        public async Task WithMessageHandler_WithMessageBodySerializer_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(messageBodySerializer: serializer);

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
        public void WithMessageHandler_WithMessageBodySerializer_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: null));
        }

        [Fact]
        public async Task WithMessageHandler_WithMessageBodySerializerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer);

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
        public void WithMessageHandler_WithMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: null));
        }

        [Fact]
        public async Task WithMessageHandler_WithMessageBodySerializerWithMessageHandlerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageBodySerializer: serializer,
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandler_WithoutMessageBodySerializerWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: null,
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: new TestMessageBodySerializer(),
                    implementationFactory: null));
        }

        [Fact]
        public async Task WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandler_WithoutMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: null));
        }
    }
}
