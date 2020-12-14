using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.Abstractions.Extensions
{
    public class MessageContextMessageSerializerMessageBodyIServiceCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerMessageBodyFilter_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutContextFilterWithMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithoutMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerImplementationFactoryMessageBodyFilter_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerWithMessageBodyFilter_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithoutContextFilterWithMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithoutMessageBodySerializerWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithoutContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => true,
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                },
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutContextFilterWithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithoutMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutContextFilterWithMessageBodySerializerWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializer: new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = new MessageContext("message-id", new Dictionary<string, object>());
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new DefaultTestMessageHandler();

            // Act
            services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                },
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithoutContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    messageHandlerImplementationFactory: serviceProvider => new DefaultTestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerDefaultContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializer: serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                },
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithoutContextFilterWithMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializer:new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithoutMessageBodySerializerWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializer: null,
                    messageBodyFilter: body => true,
                    implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializer:new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    implementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializer:new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    implementationFactory: null));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_UsesFilter(bool matchesContext, bool matchesBody)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedBody = $"test-message-body-{Guid.NewGuid()}";
            var expectedMessage = new TestMessage();
            var expectedContext = TestMessageContext.Generate();
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);
            var expectedHandler = new TestMessageHandler();

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                messageBodySerializerImplementationFactory: serviceProvider => serializer,
                messageBodyFilter: body =>
                {
                    Assert.Same(expectedMessage, body);
                    return matchesBody;
                },
                messageHandlerImplementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesBody, handler.CanProcessMessageBasedOnMessage(expectedMessage));
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(expectedBody);
            Assert.NotNull(result);
            Assert.Same(expectedMessage, result.DeserializedMessage);
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithoutContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: null,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithoutMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: null,
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithoutMessageBodyFilterWithMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: null,
                    messageHandlerImplementationFactory: serviceProvider => new TestMessageHandler()));
        }

        [Fact]
        public void WithMessageHandlerCustomContext_WithContextFilterWithMessageBodySerializerImplementationFactoryWithMessageBodyFilterWithoutMessageHandlerImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                    messageContextFilter: context => false,
                    messageBodySerializerImplementationFactory: serviceProvider => new TestMessageBodySerializer(),
                    messageBodyFilter: body => true,
                    messageHandlerImplementationFactory: null));
        }
    }
}
