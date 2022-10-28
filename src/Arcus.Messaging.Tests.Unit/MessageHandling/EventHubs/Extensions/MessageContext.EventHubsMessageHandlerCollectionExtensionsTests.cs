using System;
using System.Collections.Generic;
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
        [InlineData(false)]
        [InlineData(true)]
        public void WithEventHubsMessageHandler_WithMessageContextFilter_UsesFilter(bool matchesContext)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                });

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithMessageContextFilter_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithEventHubsMessageHandler_WithMessageContextFilterWithImplementationFactory_UsesFilter(bool matchesContext)
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());
            var order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            AzureEventHubsMessageContext expectedContext = eventData.GetMessageContext("namespace", "consumergroup", "name");
            var expectedHandler = new TestEventHubsMessageHandler();

            // Act
            collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
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
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithoutMessageContextFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    implementationFactory: serviceProvider => new TestEventHubsMessageHandler()));
        }

        [Fact]
        public void WithEventHubsMessageHandler_WithMessageContextFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var collection = new EventHubsMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithEventHubsMessageHandler<TestEventHubsMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    implementationFactory: null));
        }
    }
}
