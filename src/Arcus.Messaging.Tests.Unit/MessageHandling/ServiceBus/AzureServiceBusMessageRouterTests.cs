using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Stubs;
using Arcus.Testing.Logging;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Xunit;
using Order = Arcus.Messaging.Tests.Core.Messages.v1.Order;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus
{
    public class AzureServiceBusMessageRouterTests
    {
        [Fact]
        public async Task WithServiceBusMessageRouting_WithGeneralRouting_GoesThroughRegisteredMessageHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            var spyLogger = new InMemoryLogger();
            services.AddLogging(logging => logging.AddProvider(new CustomLoggerProvider(spyLogger)));

            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new TestServiceBusMessageHandler();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(serviceProvider => spyHandler)
                      .WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(serviceProvider => ignoredHandler);
            
            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            string json = JsonConvert.SerializeObject(order);

            await router.RouteMessageAsync(json, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
            Assert.Contains(spyLogger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Azure Service Bus from Process completed"));
        }

        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandler_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new TestServiceBusMessageHandler();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(serviceProvider => spyHandler)
                      .WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(serviceProvider => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();

            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandlerContextFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          messageContextFilter: ctx => true, implementationFactory: serviceProvider => spyHandler)
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          messageContextFilter: ctx => false, implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandlerMessageBodyFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          messageBodyFilter: body => true, implementationFactory: serviceProvider => spyHandler)
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          messageBodyFilter: body => false, implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusMessageRouting_WithMessageHandlerWithoutMessageFilterShouldGoBefore_OtherwiseDoesntTakeIntoAccountTrailingRegistrations()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(implementationFactory: serviceProvider => spyHandler)
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(messageBodyFilter: body => true, implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusRouting_WithMessageHandlerMessageBodySerializer_DeserializesCustomMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<TestMessage>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, OrderGenerator.Generate());
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(messageBodySerializer: serializer, implementationFactory: serviceProvider => spyHandler)
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            ServiceBusReceivedMessage message = expectedMessage.AsServiceBusReceivedMessage();
            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusRouting_WithMessageHandlerMessageBodySerializerSubType_DeserializesCustomMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<TestMessage>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, new SubOrder());
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(messageBodySerializer: serializer, implementationFactory: serviceProvider => spyHandler)
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(implementationFactory: serviceProvider => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            ServiceBusReceivedMessage message = expectedMessage.AsServiceBusReceivedMessage();
            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);

            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusRouting_WithMessageHandlerCanHandleAllFiltersAtOnce_AndStillFindsTheRightMessageHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler1 = new StubServiceBusMessageHandler<TestMessage>();
            var ignoredHandler2 = new StubServiceBusMessageHandler<TestMessage>();
            var ignoredHandler3 = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, OrderGenerator.Generate());
            
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
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
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            ServiceBusReceivedMessage message = expectedMessage.AsServiceBusReceivedMessage();
            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);

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
            var collection = new ServiceBusMessageHandlerCollection(services);
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>();
            
            // Act
            services.AddServiceBusMessageRouting(serviceProvider =>
                new TestAzureServiceBusMessageRouter(serviceProvider, NullLogger.Instance));
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.IsType<TestAzureServiceBusMessageRouter>(provider.GetRequiredService<IAzureServiceBusMessageRouter>());
            Assert.IsType<TestAzureServiceBusMessageRouter>(provider.GetRequiredService<IMessageRouter>());
        }

        [Theory]
        [InlineData(AdditionalMemberHandling.Ignore)]
        [InlineData(AdditionalMemberHandling.Error)]
        public async Task WithServiceBusRouting_IgnoreMissingMembers_ResultsInDifferentMessageHandler(AdditionalMemberHandling additionalMemberHandling)
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var messageHandlerV1 = new OrderV1AzureServiceBusMessageHandler();
            var messageHandlerV2 = new OrderV2AzureServiceBusMessageHandler();
            collection.WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(provider => messageHandlerV1)
                      .WithServiceBusMessageHandler<OrderV2AzureServiceBusMessageHandler, OrderV2>(provider => messageHandlerV2);
            
            // Act
            services.AddServiceBusMessageRouting(options => options.Deserialization.AdditionalMembers = additionalMemberHandling);
            
            // Assert
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var router = serviceProvider.GetRequiredService<IAzureServiceBusMessageRouter>();

            OrderV2 orderV2 = OrderV2Generator.Generate();
            ServiceBusReceivedMessage message = orderV2.AsServiceBusReceivedMessage();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None);
            
            Assert.Equal(additionalMemberHandling is AdditionalMemberHandling.Error, messageHandlerV2.IsProcessed);
            Assert.Equal(additionalMemberHandling is AdditionalMemberHandling.Ignore, messageHandlerV1.IsProcessed);
        }

        [Fact]
        public async Task WithServiceBusRouting_ExtremeNumberOfMessages_StillUsesTheCorrectMessageHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);

            var spyOrderV1MessageHandler = new OrderV1AzureServiceBusMessageHandler();
            var spyOrderV2MessageHandler = new OrderV2AzureServiceBusMessageHandler();
            
            collection.WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(messageBodyFilter: order => order.ArticleNumber == "NotExisting")
                      .WithServiceBusMessageHandler<OrderV2AzureServiceBusMessageHandler, OrderV2>(implementationFactory: provider => spyOrderV2MessageHandler)
                      .WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>()
                      .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(messageContextFilter: context => context.MessageId == "NotExisting")
                      .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(provider => spyOrderV1MessageHandler)
                      .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>();

            // Act
            services.AddServiceBusMessageRouting();
            
            // Assert
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var router = serviceProvider.GetRequiredService<IAzureServiceBusMessageRouter>();

            int messageCount = 100;
            IEnumerable<ServiceBusReceivedMessage> messages =
                Enumerable.Range(0, messageCount)
                          .SelectMany(index =>
                          {
                              var orderV1 = OrderGenerator.Generate().AsServiceBusReceivedMessage();
                              var orderV2 = OrderV2Generator.Generate().AsServiceBusReceivedMessage();
                              return new[] {orderV1, orderV2};
                          });

            foreach (ServiceBusReceivedMessage message in messages)
            {
                await router.RouteMessageAsync(message,
                    new AzureServiceBusMessageContext(
                        $"id-{Guid.NewGuid()}",
                        $"job-{Guid.NewGuid()}",
                        AzureServiceBusSystemProperties.CreateFrom(message),
                        new ReadOnlyDictionary<string, object>(new Dictionary<string, object>())),
                    new MessageCorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}"),
                    CancellationToken.None);
            }
            
            Assert.Equal(messageCount, spyOrderV1MessageHandler.ProcessedMessages.Length);
            Assert.Equal(messageCount, spyOrderV2MessageHandler.ProcessedMessages.Length);
        }



        [Fact]
        public async Task Route_WithoutAnyRegisteredMessageHandlers_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task Route_WithoutAnyMatchingRegisteredMessageHandlers_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddServiceBusMessageRouting()
                    .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            OrderV2 order = OrderV2Generator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => router.RouteMessageAsync(message, context, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task Route_WithMessageHandlerFromTemplate_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddServiceBusMessageRouting()
                    .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandlerFromTemplate, Order>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var receiver = new TestServiceBusReceiver();
            await router.RouteMessageAsync(receiver, message, context, correlationInfo, CancellationToken.None);
            Assert.True(receiver.HasCompletedMessage);
        }

        [Fact]
        public async Task Route_WithFallbackMessageHandlerFromTemplate_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddServiceBusMessageRouting()
                    .WithServiceBusFallbackMessageHandler<TestAzureServiceBusFallbackMessageHandlerFromTemplate>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var receiver = new TestServiceBusReceiver();
            await router.RouteMessageAsync(receiver, message, context, correlationInfo, CancellationToken.None);
            Assert.True(receiver.HasCompletedMessage);
        }

        [Fact]
        public void CreateWithoutOptionsAndLogger_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusMessageRouter(serviceProvider: null));
        }

        [Fact]
        public void CreateWithoutOptions_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusMessageRouter(serviceProvider: null, logger: NullLogger<AzureServiceBusMessageRouter>.Instance));
        }

        [Fact]
        public void CreateWithoutLogger_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusMessageRouter(serviceProvider: null, options: new AzureServiceBusMessageRouterOptions()));
        }

        [Fact]
        public void Create_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusMessageRouter(
                    serviceProvider: null,
                    options: new AzureServiceBusMessageRouterOptions(),
                    logger: NullLogger<AzureServiceBusMessageRouter>.Instance));
        }
    }
}