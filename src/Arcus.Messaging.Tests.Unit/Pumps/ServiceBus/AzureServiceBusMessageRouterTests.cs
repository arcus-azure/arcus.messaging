using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Messaging.Tests.Unit.Pumps.ServiceBus.Stubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using Order = Arcus.Messaging.Tests.Core.Messages.v1.Order;

namespace Arcus.Messaging.Tests.Unit.Pumps.ServiceBus
{
    public class AzureServiceBusMessageRouterTests
    {
        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandler_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredHandler = new TestServiceBusMessageHandler();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            services.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(serviceProvider => spyHandler)
                    .WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(serviceProvider => ignoredHandler);

            // Act
            services.WithServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            Message message = order.AsServiceBusMessage();
            await router.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandlerContextFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            services.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                        messageContextFilter: ctx => true, implementationFactory: serviceProvider => spyHandler)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                        messageContextFilter: ctx => false, implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.WithServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            Message message = order.AsServiceBusMessage();
            await router.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandlerMessageBodyFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            services.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                        messageBodyFilter: body => true, implementationFactory: serviceProvider => spyHandler)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                        messageBodyFilter: body => false, implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.WithServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            Message message = order.AsServiceBusMessage();
            await router.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandlerWithoutMessageFilterShouldGoBefore_OtherwiseDoesntTakeIntoAccountTrailingRegistrations()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            services.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(implementationFactory: serviceProvider => spyHandler)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(messageBodyFilter: body => true, implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.WithServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            Message message = order.AsServiceBusMessage();
            await router.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusRouting_WithMessageHandlerMessageBodySerializer_DeserializesCustomMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredHandler = new StubServiceBusMessageHandler<TestMessage>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, OrderGenerator.Generate());
            services.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(messageBodySerializer: serializer, implementationFactory: serviceProvider => spyHandler)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.WithServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Message message = expectedMessage.AsServiceBusMessage();
            await router.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusRouting_WithMessageHandlerMessageBodySerializerSubType_DeserializesCustomMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredHandler = new StubServiceBusMessageHandler<TestMessage>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, new SubOrder());
            services.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(messageBodySerializer: serializer, implementationFactory: serviceProvider => spyHandler)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.WithServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Message message = expectedMessage.AsServiceBusMessage();
            await router.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusRouting_WithMessageHandlerCanHandleAllFiltersAtOnce_AndStillFindsTheRightMessageHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredHandler1 = new StubServiceBusMessageHandler<TestMessage>();
            var ignoredHandler2 = new StubServiceBusMessageHandler<TestMessage>();
            var ignoredHandler3 = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, OrderGenerator.Generate());
            
            services
                .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                        messageContextFilter: ctx => ctx != null,
                        messageBodyFilter: body => body != null,
                        messageBodySerializer: new TestMessageBodySerializer(expectedBody, new Customer()),
                        implementationFactory: serviceProvider => ignoredHandler3)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(
                        messageBodyFilter: body => body is null,
                        implementationFactory: serviceProvider => ignoredHandler2)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                        messageBodySerializer: serializer, 
                        messageBodyFilter: body => body.Customer != null,
                        messageContextFilter: ctx => ctx.MessageId.StartsWith("message-id"),
                        implementationFactory: serviceProvider => spyHandler)
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>()
                    .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(
                        implementationFactory: serviceProvider => ignoredHandler1);

            // Act
            services.WithServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            Message message = expectedMessage.AsServiceBusMessage();
            await router.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler1.IsProcessed);
            Assert.False(ignoredHandler2.IsProcessed);
            Assert.False(ignoredHandler3.IsProcessed);
        }

        [Fact]
        public void WithServiceBusRouting_WithCustomRouter_RegistersCustomRouter()
        {
            // Arrange
            var services = new ServiceCollection();
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>();
            
            // Act
            services.WithServiceBusMessageRouting(serviceProvider =>
                new TestAzureServiceBusMessageRouter(serviceProvider, NullLogger.Instance));
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.IsType<TestAzureServiceBusMessageRouter>(provider.GetRequiredService<IAzureServiceBusMessageRouter>());
            Assert.IsType<TestAzureServiceBusMessageRouter>(provider.GetRequiredService<IMessageRouter>());
        }
    }
}