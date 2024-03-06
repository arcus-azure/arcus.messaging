using System;
using System.Collections.Generic;
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
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithEventHubsMessageHandler_WithMessageContextFilterWithMessageBodyFilter_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
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
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithMessageContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithEventHubsMessageHandler_WithMessageContextFilterWithMessageBodyFilterWithImplementationFactory_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var expectedMessage = new TestMessage();
            var eventData = new EventData(JsonConvert.SerializeObject(expectedMessage));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
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
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithoutMessageContextFilterWithMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithMessageContextFilterWithoutMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithMessageContextFilterWithMessageBodyFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodyFilter: body => false,
                    implementationFactory: null));
        }
    }
}
