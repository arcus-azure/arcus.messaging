using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
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
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithoutMessageContextFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageContextFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithMessageContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(context => true, messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(messageContextFilter: null, messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithImplementationFactoryWithMessageContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    context => true, messageBodyFilter: null, implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessage_WithImplementationFactoryWithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null, messageBodyFilter: body => true, implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithoutMessageContextFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(context => true, messageBodyFilter: null));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(messageContextFilter: null, messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithImplementationFactoryWithContextFilterWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    context => true, messageBodyFilter: null, implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerTMessageHandlerTMessageTMessageContext_WithImplementationFactoryWithoutMessageContextFilterWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null, messageBodyFilter: body => true, implementationFactory: serviceProvider=> new TestMessageHandler()));
        }

        [Fact]
        public void WithFallbackMessageHandler_WithValidType_RegistersInterface()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var messageHandler = provider.GetRequiredService<FallbackMessageHandler<string, MessageContext>>();

            Assert.IsType<PassThruFallbackMessageHandler>(messageHandler.MessageHandlerInstance);
        }

        [Fact]
        public void WithFallbackMessageHandler_WithValidImplementationFunction_RegistersInterface()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expected = new PassThruFallbackMessageHandler();

            // Act
            services.WithFallbackMessageHandler(serviceProvider => expected);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var actual = provider.GetRequiredService<FallbackMessageHandler<string, MessageContext>>();

            Assert.Same(expected, actual.MessageHandlerInstance);
        }

        [Fact]
        public void WithFallbackMessageHandlerT_WithValidImplementationFunction_RegistersInterface()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            var expected = new PassThruFallbackMessageHandler<TestMessageContext>();

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler<TestMessageContext>, TestMessageContext>(serviceProvider => expected);

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var actual = provider.GetRequiredService<FallbackMessageHandler<string, TestMessageContext>>();

            Assert.Same(expected, actual.MessageHandlerInstance);
        }

        [Fact]
        public void WithFallbackMessageHandlerType_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((MessageHandlerCollection) null).WithFallbackMessageHandler<PassThruFallbackMessageHandler>());
        }

        [Fact]
        public void WithFallbackMessageHandlerImplementationFunction_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((MessageHandlerCollection) null).WithFallbackMessageHandler(serviceProvider => new PassThruFallbackMessageHandler()));
        }

        [Fact]
        public void WithFallbackMessageHandlerImplementationFunction_WithoutImplementationFunction_Throws()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithFallbackMessageHandler(createImplementation: (Func<IServiceProvider, PassThruFallbackMessageHandler>) null));
        }

        [Fact]
        public void WithFallbackMessageHandlerTImplementationFunction_WithoutImplementationFunction_Throws()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithFallbackMessageHandler<PassThruFallbackMessageHandler<TestMessageContext>, TestMessageContext>(createImplementation: null));
        }
    }
}
