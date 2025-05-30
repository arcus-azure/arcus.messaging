using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Extensions
{
    public partial class ServiceBusMessageHandlerCollectionExtensionsTests
    {
        [Fact]
        public async Task WithMessageHandler_WithMessageBodySerializer_UsesSerializer()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(options => options.AddMessageBodySerializer(serializer));

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public async Task WithMessageHandler_WithMessageBodySerializerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                options => options.AddMessageBodySerializer(_ => serializer));

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public async Task WithMessageHandler_WithMessageBodySerializerWithMessageHandlerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                implementationFactory: _ => expectedHandler,
                options => options.AddMessageBodySerializer(serializer));

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
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
        public async Task WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_UsesSerializer()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                implementationFactory: _ => expectedHandler,
                options => options.AddMessageBodySerializer(_ => serializer));

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }
    }
}
