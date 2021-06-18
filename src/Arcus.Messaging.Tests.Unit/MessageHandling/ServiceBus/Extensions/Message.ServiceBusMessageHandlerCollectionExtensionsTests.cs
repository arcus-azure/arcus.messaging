using System;
using System.Collections.Generic;
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
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithServiceBusMessageHandler_WithMessageBodyFilter_UsesFilter(bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedMessage = new TestMessage();
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
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
            Assert.NotSame(expectedHandler, handler);
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithMessageBodyFilter_Fails()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithServiceBusMessageHandler_WithMessageBodyFilterWithImplementationFactory_UsesFilter(bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedMessage = new TestMessage();
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
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
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithMessageBodyFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }
    }
}
