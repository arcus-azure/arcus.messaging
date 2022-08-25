using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Fixture;
using Azure.Messaging.EventHubs;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Extensions
{
    public partial class EventHubsMessageHandlerCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageContextFilterWithMessageBodySerializer_UsesSerializer(bool matches)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            var expectedContext = AzureEventHubsMessageContext.CreateFrom(eventData, "namespace", "consumer-group", "eventhubs name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializer: serializer);

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageContextFilterWithMessageBodySerializer_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageContextFilterWithoutMessageBodySerializer_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageContextFilterWithMessageBodySerializerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            var expectedContext = AzureEventHubsMessageContext.CreateFrom(eventData, "namespace", "consumer-group", "eventhubs name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer);

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageContextFilterWithoutMessageBodySerializerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageContextFilterWithMessageBodySerializerWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            var expectedContext = AzureEventHubsMessageContext.CreateFrom(eventData, "namespace", "consumer-group", "eventhubs name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializer: serializer,
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageContextFilterWithMessageBodySerializerWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageContextFilterWithoutMessageBodySerializerWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null,
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageContextFilterWithMessageBodySerializerWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    implementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageContextFilterWithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            var expectedContext = AzureEventHubsMessageContext.CreateFrom(eventData, "namespace", "consumer-group", "eventhubs name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matches;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageContextFilterWithMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageHandlerImplementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(), 
                    messageHandlerImplementationFactory: null));
        }
    }
}
