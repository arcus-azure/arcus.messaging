using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.Extensions
{
    public partial class MessageHandlerCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializerWithMessageBodyFilter_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                });

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
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

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializer: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

         [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                });

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
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

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                },
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
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

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializer: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => false,
                    implementationFactory: null));
        }

         [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                },
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
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

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerWithCustomContext_WithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: null));
        }
    }
}
