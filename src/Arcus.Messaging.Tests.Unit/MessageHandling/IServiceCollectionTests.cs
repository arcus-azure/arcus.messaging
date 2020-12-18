using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class IServiceCollectionTests
    {
        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithoutMessageContextFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageContextFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithMessageContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(context => true, messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageContextFilter: null, messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithImplementationFactoryWithMessageContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    context => true, messageBodyFilter: null, implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithImplementationFactoryWithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null, messageBodyFilter: body => true, implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithoutMessageContextFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(context => true, messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: null, messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithImplementationFactoryWithContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    context => true, messageBodyFilter: null, implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithImplementationFactoryWithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null, messageBodyFilter: body => true, implementationFactory: serviceProvider=> new TestMessageHandler()));
        }

        [Fact]
        public void WithFallbackMessageHandler_WithValidType_RegistersInterface()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var messageHandler = provider.GetRequiredService<IFallbackMessageHandler>();

            Assert.IsType<PassThruFallbackMessageHandler>(messageHandler);
        }

        [Fact]
        public void WithFallbackMessageHandler_WithValidImplementationFunction_RegistersInterface()
        {
            // Arrange
            var services = new ServiceCollection();
            var expected = new PassThruFallbackMessageHandler();

            // Act
            services.WithFallbackMessageHandler(serviceProvider => expected);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var actual = provider.GetRequiredService<IFallbackMessageHandler>();

            Assert.Same(expected, actual);
        }

        [Fact]
        public void WithFallbackMessageHandlerType_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((IServiceCollection) null).WithFallbackMessageHandler<PassThruFallbackMessageHandler>());
        }

        [Fact]
        public void WithFallbackMessageHandlerImplementationFunction_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((IServiceCollection) null).WithFallbackMessageHandler(serviceProvider => new PassThruFallbackMessageHandler()));
        }

        [Fact]
        public void WithFallbackMessageHandlerImplementationFunction_WithoutImplementationFunction_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithFallbackMessageHandler(createImplementation: (Func<IServiceProvider, PassThruFallbackMessageHandler>) null));
        }
    }
}
