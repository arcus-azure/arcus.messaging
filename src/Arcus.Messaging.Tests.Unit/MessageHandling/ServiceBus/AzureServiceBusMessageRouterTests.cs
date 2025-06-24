using System;
using System.Collections.Generic;
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
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Order = Arcus.Messaging.Tests.Core.Messages.v1.Order;
using OrderV2AzureServiceBusMessageHandler = Arcus.Messaging.Tests.Unit.Fixture.OrderV2AzureServiceBusMessageHandler;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus
{
    public class AzureServiceBusMessageRouterTests
    {
        [Fact]
        public async Task RouteMessage_WithDifferentMessageContext_SucceedsWithSameJobId()
        {
            // Arrange
            var services = new ServiceCollection();
            ServiceBusMessageHandlerCollection collection = services.AddServiceBusMessageRouting();
            var jobId = Guid.NewGuid().ToString();
            collection.JobId = jobId;

            // Act
            collection.WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            var order = OrderGenerator.Generate();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(order), messageId: "message-id");
            var context = AzureServiceBusMessageContext.Create(jobId, ServiceBusEntityType.Unknown, Mock.Of<ServiceBusReceiver>(), message);
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
        }

        [Fact]
        public async Task RouteMessage_WithoutFallbackWithFailingButMatchingMessageHandler_PassThruMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var sabotageHandler = new OrdersSabotageAzureServiceBusMessageHandler();

            services.AddServiceBusMessageRouting()
                    .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                    .WithServiceBusMessageHandler<OrdersSabotageAzureServiceBusMessageHandler, Order>(_ => sabotageHandler);

            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            var receiver = new Mock<ServiceBusReceiver>();
            var order = OrderGenerator.Generate();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(order), messageId: "message-id");
            var context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = message.GetCorrelationInfo();

            // Act / Assert
            MessageProcessingResult result = await router.RouteMessageAsync(receiver.Object, message, context, correlationInfo, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccessful, result.ErrorMessage);
            Assert.True(sabotageHandler.IsProcessed, "sabotage message handler should be tried");
        }

        [Fact]
        public async Task RouteMessage_WithMessageHandler_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new TestServiceBusMessageHandler();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(_ => spyHandler)
                      .WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(_ => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();

            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.True(spyHandler.IsProcessed, result.ErrorMessage);
            Assert.False(ignoredHandler.IsProcessed, result.ErrorMessage);
        }

        [Fact]
        public async Task RouteMessage_WithMessageHandlerContextFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          implementationFactory: _ => spyHandler, 
                          opt => opt.AddMessageContextFilter(_ => true))
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          implementationFactory: _ => ignoredHandler, 
                          opt => opt.AddMessageContextFilter(_ => false));

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task RouteMessage_WithMessageHandlerMessageBodyFilter_GoesThroughRegisteredMessageHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          implementationFactory: _ => spyHandler, opt => opt.AddMessageBodyFilter(_ => true))
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          implementationFactory: _ => ignoredHandler, opt => opt.AddMessageBodyFilter(_ => false));

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task RouteMessage_WithMessageHandlerWithoutMessageFilterShouldGoBefore_OtherwiseDoesntTakeIntoAccountTrailingRegistrations()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<Order>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(implementationFactory: _ => spyHandler)
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(implementationFactory: _ => ignoredHandler, opt => opt.AddMessageBodyFilter(_ => true));

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage();
            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task RouteMessage_WithMessageHandlerMessageBodySerializer_DeserializesCustomMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<TestMessage>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, OrderGenerator.Generate());
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(implementationFactory: _ => spyHandler, options => options.AddMessageBodySerializer(serializer))
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(implementationFactory: _ => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            ServiceBusReceivedMessage message = expectedMessage.AsServiceBusReceivedMessage();
            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task RouteMessage_WithMessageHandlerMessageBodySerializerSubType_DeserializesCustomMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var ignoredHandler = new StubServiceBusMessageHandler<TestMessage>();
            var spyHandler = new StubServiceBusMessageHandler<Order>();

            var expectedMessage = new TestMessage { TestProperty = "Some value" };
            string expectedBody = JsonConvert.SerializeObject(expectedMessage);
            var serializer = new TestMessageBodySerializer(expectedBody, new SubOrder());
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(implementationFactory: _ => spyHandler, options => options.AddMessageBodySerializer(serializer))
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(implementationFactory: _ => ignoredHandler);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            ServiceBusReceivedMessage message = expectedMessage.AsServiceBusReceivedMessage();
            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler.IsProcessed);
        }

        [Fact]
        public async Task RouteMessage_WithMessageHandlerCanHandleAllFiltersAtOnce_AndStillFindsTheRightMessageHandler()
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
                          implementationFactory: _ => ignoredHandler3,
                          options => options.AddMessageContextFilter(ctx => ctx != null)
                                            .AddMessageBodyFilter(body => body != null)
                                            .AddMessageBodySerializer(new TestMessageBodySerializer(expectedBody, new Customer())))
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(
                          implementationFactory: _ => ignoredHandler2,
                          opt => opt.AddMessageBodyFilter(body => body is null))
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<Order>, Order>(
                          implementationFactory: _ => spyHandler,
                          options => options.AddMessageBodySerializer(serializer)
                                            .AddMessageBodyFilter(body => body.Customer != null)
                                            .AddMessageContextFilter(ctx => ctx.MessageId.StartsWith("message-id")))
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>()
                      .WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(
                          implementationFactory: _ => ignoredHandler1);

            // Act
            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            ServiceBusReceivedMessage message = expectedMessage.AsServiceBusReceivedMessage();
            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.True(spyHandler.IsProcessed);
            Assert.False(ignoredHandler1.IsProcessed);
            Assert.False(ignoredHandler2.IsProcessed);
            Assert.False(ignoredHandler3.IsProcessed);
        }

        [Fact]
        public void RouteMessage_WithCustomRouter_RegistersCustomRouter()
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
        }

        [Theory]
        [InlineData(AdditionalMemberHandling.Ignore)]
        [InlineData(AdditionalMemberHandling.Error)]
        public async Task RouteMessage_IgnoreMissingMembers_ResultsInDifferentMessageHandler(AdditionalMemberHandling additionalMemberHandling)
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);
            var messageHandlerV1 = new OrderV1AzureServiceBusMessageHandler();
            var messageHandlerV2 = new OrderV2AzureServiceBusMessageHandler();
            collection.WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(_ => messageHandlerV1)
                      .WithServiceBusMessageHandler<OrderV2AzureServiceBusMessageHandler, OrderV2>(_ => messageHandlerV2);

            // Act
            services.AddServiceBusMessageRouting(options => options.Deserialization.AdditionalMembers = additionalMemberHandling);

            // Assert
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var router = serviceProvider.GetRequiredService<IAzureServiceBusMessageRouter>();

            OrderV2 orderV2 = OrderV2Generator.Generate();
            ServiceBusReceivedMessage message = orderV2.AsServiceBusReceivedMessage();
            AzureServiceBusMessageContext context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            MessageProcessingResult result = await router.RouteMessageAsync(Mock.Of<ServiceBusReceiver>(), message, context, correlationInfo, CancellationToken.None);
            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.Equal(additionalMemberHandling is AdditionalMemberHandling.Error, messageHandlerV2.IsProcessed);
            Assert.Equal(additionalMemberHandling is AdditionalMemberHandling.Ignore, messageHandlerV1.IsProcessed);
        }

        [Fact]
        public async Task RouteMessage_ExtremeNumberOfMessages_StillUsesTheCorrectMessageHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new ServiceBusMessageHandlerCollection(services);

            var spyOrderV1MessageHandler = new OrderV1AzureServiceBusMessageHandler();
            var spyOrderV2MessageHandler = new OrderV2AzureServiceBusMessageHandler();

            collection.WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(opt => opt.AddMessageBodyFilter(order => order.ArticleNumber == "NotExisting"))
                      .WithServiceBusMessageHandler<OrderV2AzureServiceBusMessageHandler, OrderV2>(implementationFactory: _ => spyOrderV2MessageHandler)
                      .WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>()
                      .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(opt => opt.AddMessageContextFilter(context => context.MessageId == "NotExisting"))
                      .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>(_ => spyOrderV1MessageHandler)
                      .WithServiceBusMessageHandler<OrderV1AzureServiceBusMessageHandler, Order>();

            services.AddServiceBusMessageRouting();

            // Assert
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var router = serviceProvider.GetRequiredService<IAzureServiceBusMessageRouter>();

            int messageCount = 100;
            IEnumerable<ServiceBusReceivedMessage> messages =
                Enumerable.Range(0, messageCount)
                          .SelectMany(_ =>
                          {
                              var orderV1 = OrderGenerator.Generate().AsServiceBusReceivedMessage();
                              var orderV2 = OrderV2Generator.Generate().AsServiceBusReceivedMessage();
                              return new[] { orderV1, orderV2 };
                          });

            foreach (ServiceBusReceivedMessage message in messages)
            {
                await router.RouteMessageAsync(
                    Mock.Of<ServiceBusReceiver>(),
                    message,
                    AzureServiceBusMessageContext.Create(
                        $"job-{Guid.NewGuid()}",
                        ServiceBusEntityType.Unknown,
                        Mock.Of<ServiceBusReceiver>(),
                        ServiceBusModelFactory.ServiceBusReceivedMessage(
                            messageId: $"id-{Guid.NewGuid()}")),
                    new MessageCorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}"),
                    CancellationToken.None);
            }

            Assert.Equal(messageCount, spyOrderV1MessageHandler.ProcessedMessages.Length);
            Assert.Equal(messageCount, spyOrderV2MessageHandler.ProcessedMessages.Length);
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