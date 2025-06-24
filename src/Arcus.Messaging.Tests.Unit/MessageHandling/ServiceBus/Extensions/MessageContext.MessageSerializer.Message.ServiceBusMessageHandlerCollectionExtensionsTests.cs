using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async Task WithServiceBusMessageHandler_WithContextFilterWithMessageBodySerializerWithMessageBodyFilter_UsesAll(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                options => options.AddMessageContextFilter(context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                }).AddMessageBodySerializer(serializer)
                  .AddMessageBodyFilter(body =>
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
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithServiceBusMessageHandler_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_UsesAll(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                options => options.AddMessageContextFilter(context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                }).AddMessageBodySerializer(serviceProvider => serializer)
                  .AddMessageBodyFilter(body =>
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
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithServiceBusMessageHandler_WithContextFilterWithMessageBodySerializerWithMessageBodyFilterWithMessagehandlerImplementationFactory_UsesAll(
            bool matchesContext, 
            bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                implementationFactory: serviceProvider => expectedHandler,
                options => options.AddMessageContextFilter(context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                }).AddMessageBodySerializer(serializer)
                .AddMessageBodyFilter(body =>
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
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithServiceBusMessageHandler_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesAll(
            bool matchesContext, 
            bool matchesBody)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                implementationFactory: _ => expectedHandler,
                options => options.AddMessageContextFilter(context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                }).AddMessageBodySerializer(_ => serializer)
                  .AddMessageBodyFilter(body =>
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
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }
    }
}
