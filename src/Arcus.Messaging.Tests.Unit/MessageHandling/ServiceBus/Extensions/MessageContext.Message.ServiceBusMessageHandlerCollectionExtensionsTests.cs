using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Stubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Extensions
{
    public partial class ServiceBusMessageHandlerCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithServiceBusMessageHandler_WithMessageContextFilterWithMessageBodyFilter_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedMessage = new TestMessage();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                options => options.AddMessageContextFilter(context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                }).AddMessageBodyFilter(body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                }));

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

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithServiceBusMessageHandler_WithMessageContextFilterWithMessageBodyFilterWithImplementationFactory_UsesSerializer(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedMessage = new TestMessage();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                implementationFactory: _ => expectedHandler,
                options => options.AddMessageContextFilter(context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                }).AddMessageBodyFilter(body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                }));

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
    }
}
