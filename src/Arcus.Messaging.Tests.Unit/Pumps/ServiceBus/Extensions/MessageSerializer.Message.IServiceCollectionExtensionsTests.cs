﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.ServiceBus.Extensions
{
    public class MessageSerializer
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageBodySerializerWithMessageBodyFilter_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: serviceProvider =>  new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                },
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesSerializer(bool matches)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matches;
                },
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
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
        public void WithMessageHandler_WithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithMessageHandler_WithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: null));
        }
    }
}
