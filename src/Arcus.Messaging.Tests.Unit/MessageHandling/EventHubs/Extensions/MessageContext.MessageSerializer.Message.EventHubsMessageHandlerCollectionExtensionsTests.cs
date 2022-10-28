using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Tests.Core.Generators;
using Azure.Messaging.EventHubs;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Extensions
{
    public partial class EventHubsMessageHandlerCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerWithMessageBodyFilter_UsesAll(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                });

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithoutContextFilterWithMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithoutMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_UsesAll(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                });

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithoutContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerWithMessageBodyFilterWithMessagehandlerImplementationFactory_UsesAll(
            bool matchesContext, 
            bool matchesBody)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                },
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithoutContextFilterWithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithoutMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesAll(
            bool matchesContext, 
            bool matchesBody)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                },
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithoutContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: null));
        }
    }
}
