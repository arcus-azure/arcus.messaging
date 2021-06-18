using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using Order = Arcus.Messaging.Tests.Core.Messages.v1.Order;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class MessageRouterTests
    {
        [Fact]
        public async Task WithMessageRouting_WithMessageHandler_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new MessageHandlerCollection(services);
            var spyHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            
            var ignoredDefaultHandler = new DefaultTestMessageHandler();
            var ignoredHandler = new TestMessageHandler();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(serviceProvider => ignoredDefaultHandler)
                      .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(serviceProvider => spyHandler)
                      .WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(serviceProvider => ignoredHandler);

            // Act
            services.AddMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var context = TestMessageContext.Generate();
            string json = JsonConvert.SerializeObject(new TestMessage());
            await router.RouteMessageAsync(json, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredDefaultHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithMessageRoutingForCustomRouter_WithMessageHandler_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new MessageHandlerCollection(services);
            var spyHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            
            var ignoredDefaultHandler = new DefaultTestMessageHandler();
            var ignoredHandler = new TestMessageHandler();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(serviceProvider => ignoredDefaultHandler)
                    .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(serviceProvider => spyHandler)
                    .WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(serviceProvider => ignoredHandler);

            // Act
            services.AddMessageRouting(serviceProvider => new TestMessageRouter(serviceProvider, NullLogger.Instance));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var context = TestMessageContext.Generate();
            string json = JsonConvert.SerializeObject(new TestMessage());
            await router.RouteMessageAsync(json, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredDefaultHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithMessageRouting_WithMessageHandlerContextFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new MessageHandlerCollection(services);
            var spyHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            var ignoredSameTypeHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            
            var ignoredDefaultHandler = new DefaultTestMessageHandler();
            var ignoredHandler = new TestMessageHandler();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(serviceProvider => ignoredDefaultHandler)
                      .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(
                          messageContextFilter: ctx => false, implementationFactory: serviceProvider => ignoredSameTypeHandler)
                      .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(serviceProvider => spyHandler)
                      .WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(serviceProvider => ignoredHandler);

            // Act
            services.AddMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var context = TestMessageContext.Generate();
            string json = JsonConvert.SerializeObject(new TestMessage());
            await router.RouteMessageAsync(json, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredSameTypeHandler.IsProcessed);
            Assert.False(ignoredDefaultHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithMessageRouting_WithMessageHandlerMessageBodyFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new MessageHandlerCollection(services);
            var spyHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            var ignoredSameTypeHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            
            var ignoredDefaultHandler = new DefaultTestMessageHandler();
            var ignoredHandler = new TestMessageHandler();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(serviceProvider => ignoredDefaultHandler)
                      .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(
                          messageBodyFilter: body => false, implementationFactory: serviceProvider => ignoredSameTypeHandler)
                      .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(serviceProvider => spyHandler)
                      .WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(serviceProvider => ignoredHandler);

            // Act
            services.AddMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var context = TestMessageContext.Generate();
            string json = JsonConvert.SerializeObject(new TestMessage());
            await router.RouteMessageAsync(json, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredSameTypeHandler.IsProcessed);
            Assert.False(ignoredDefaultHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithMessageRouting_WithMessageHandlerMessageBodySerializer_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new MessageHandlerCollection(services);
            var spyHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            var ignoredSameTypeHandler = new StubTestMessageHandler<TestMessage, TestMessageContext>();
            var ignoredWrongDeserializedTypeHandler = new StubTestMessageHandler<Core.Messages.v1.Order, TestMessageContext>();
            var ignoredDefaultHandler = new DefaultTestMessageHandler();
            var ignoredHandler = new TestMessageHandler();
            
            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, expectedMessage);

            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(serviceProvider => ignoredDefaultHandler)
                      .WithMessageHandler<StubTestMessageHandler<Core.Messages.v1.Order, TestMessageContext>, Order, TestMessageContext>(
                          messageBodySerializer: new TestMessageBodySerializer(expectedBody, new Customer()),
                          implementationFactory: serviceProvider => ignoredWrongDeserializedTypeHandler)
                      .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(
                          messageContextFilter: ctx => false, implementationFactory: serviceProvider => ignoredSameTypeHandler)
                      .WithMessageHandler<StubTestMessageHandler<TestMessage, TestMessageContext>, TestMessage, TestMessageContext>(
                          messageBodySerializer: serializer, 
                          implementationFactory: serviceProvider => spyHandler)
                      .WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(serviceProvider => ignoredHandler);

            // Act
            services.AddMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var context = TestMessageContext.Generate();
            await router.RouteMessageAsync(expectedBody, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredSameTypeHandler.IsProcessed);
            Assert.False(ignoredDefaultHandler.IsProcessed);
            Assert.False(ignoredWrongDeserializedTypeHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WithMessageRouting_WithIgnoreMissingMembers_GoesThroughDifferentMessageHandler(bool ignoreMissingMembers)
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new MessageHandlerCollection(services);
            var messageHandlerV1 = new OrderV1MessageHandler();
            var messageHandlerV2 = new OrderV2MessageHandler();

            collection.WithMessageHandler<OrderV1MessageHandler, Order>(provider => messageHandlerV1)
                      .WithMessageHandler<OrderV2MessageHandler, OrderV2>(provider => messageHandlerV2);
            
            // Act
            services.AddMessageRouting(options => options.Deserialization.IgnoreMissingMembers = ignoreMissingMembers);
            
            // Assert
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var router = serviceProvider.GetRequiredService<IMessageRouter>();

            OrderV2 order = OrderV2Generator.Generate();
            var context = new MessageContext("message-id", new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            string json = JsonConvert.SerializeObject(order);

            await router.RouteMessageAsync(json, context, correlationInfo, CancellationToken.None);
            Assert.Equal(ignoreMissingMembers, messageHandlerV1.IsProcessed);
            Assert.NotEqual(ignoreMissingMembers, messageHandlerV2.IsProcessed);
        }
        
        [Fact]
        public void CreateWithoutOptionsAndLogger_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new MessageRouter(serviceProvider: null));
        }

        [Fact]
        public void CreateWithoutOptions_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                new MessageRouter(serviceProvider: null, logger: NullLogger<MessageRouter>.Instance));
        }

        [Fact]
        public void CreateWithoutLogger_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new MessageRouter(serviceProvider: null, options: new MessageRouterOptions()));
        }

        [Fact]
        public void Create_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new MessageRouter(serviceProvider: null, options: new MessageRouterOptions(), logger: NullLogger<MessageRouter>.Instance));
        }
    }
}
