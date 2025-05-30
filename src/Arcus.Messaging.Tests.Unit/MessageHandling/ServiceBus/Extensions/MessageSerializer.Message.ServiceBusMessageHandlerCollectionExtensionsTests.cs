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
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageBodySerializerWithMessageBodyFilter_UsesSerializer(bool matches)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                options => options.AddMessageBodySerializer(serializer)
                                  .AddMessageBodyFilter(body =>
                                  {
                                      Assert.Same(expectedMessage, body);
                                      return matches;
                                  }));

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnMessage(expectedMessage);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                implementationFactory: serviceProvider => expectedHandler,
                options => options.AddMessageBodySerializer(serializer)
                                  .AddMessageBodyFilter(body =>
                                  {
                                      Assert.Same(expectedMessage, body);
                                      return matches;
                                  }));

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            bool actual = handler.CanProcessMessageBasedOnMessage(expectedMessage);
            Assert.Equal(matches, actual);
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }
    }
}
