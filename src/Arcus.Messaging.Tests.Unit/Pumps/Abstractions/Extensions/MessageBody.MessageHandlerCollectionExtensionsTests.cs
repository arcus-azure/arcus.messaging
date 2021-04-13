using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.Abstractions.Extensions
{
    public partial class MessageHandlerCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerDefaultContext_WithMessageBodyFilter_UsesFilter(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var message = new TestMessage();
            
            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageBodyFilter: body =>
            {
                Assert.Same(message, body);
                return matches;
            });

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnMessage(message);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerCustomContext_WithMessageBodyFilter_UsesFilter(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var message = new TestMessage();
            
            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageBodyFilter: body =>
            {
                Assert.Same(message, body);
                return matches;
            });

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            bool actual = handler.CanProcessMessageBasedOnMessage(message);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerDefaultContext_WithMessageBodyFilterWithImplementationFactory_UsesFilter(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedHandler = new DefaultTestMessageHandler();
            var message = new TestMessage();
            
            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageBodyFilter: body =>
            {
                Assert.Same(message, body);
                return matches;
            }, implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            bool actual = handler.CanProcessMessageBasedOnMessage(message);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageBodyFilter: null, implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithMessageBodyFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageBodyFilter: body => true, implementationFactory: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithMessageHandlerCustomContext_WithMessageBodyFilterWithImplementationFactory_UsesFilter(bool matches)
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expectedHandler = new TestMessageHandler();
            var message = new TestMessage();
            
            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageBodyFilter: body =>
            {
                Assert.Same(message, body);
                return matches;
            }, implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            bool actual = handler.CanProcessMessageBasedOnMessage(message);
            Assert.Equal(matches, actual);
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithoutMessageBodyFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodyFilter: null, implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithMessageBodyFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageBodyFilter: body => false, implementationFactory: null));
        }
    }
}
