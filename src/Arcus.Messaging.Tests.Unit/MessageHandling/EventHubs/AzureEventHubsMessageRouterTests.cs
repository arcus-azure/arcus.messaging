using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Unit.EventHubs.Fixture;
using Arcus.Messaging.Tests.Unit.Fixture;
#if NET6_0
using Arcus.Messaging.Abstractions.EventHubs;
using Azure.Messaging.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Fixture;
using Newtonsoft.Json;
#endif
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ArgumentException = System.ArgumentException;
using Order = Arcus.Messaging.Tests.Core.Messages.v1.Order;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs
{
#if NET6_0
    public class AzureEventHubsMessageRouterTests
    {
        [Fact]
        public async Task Add_WithDifferentMessageContext_SucceedsWithSameJobId()
        {
            // Arrange
            var services = new ServiceCollection();
            EventHubsMessageHandlerCollection collection = services.AddEventHubsMessageRouting();
            var jobId = Guid.NewGuid().ToString();
            collection.JobId = jobId;

            // Act
            collection.WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureEventHubsMessageRouter>();

            var order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name", jobId);
            MessageCorrelationInfo correlationInfo = eventData.GetCorrelationInfo();

            await router.RouteMessageAsync(eventData, context, correlationInfo, CancellationToken.None);
        }

        [Fact]
        public async Task Add_WithDifferentMessageContext_FailsWithDifferentJobId()
        {
            // Arrange
            var services = new ServiceCollection();
            EventHubsMessageHandlerCollection collection = services.AddEventHubsMessageRouting();
            collection.JobId = Guid.NewGuid().ToString();

            // Act
            collection.WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureEventHubsMessageRouter>();

            var order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "name", "consumer-group", "other-job-id");
            MessageCorrelationInfo correlationInfo = eventData.GetCorrelationInfo();

            await Assert.ThrowsAsync<InvalidOperationException>(() => router.RouteMessageAsync(eventData, context, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task WithEventHubsMessageRouting_WithMultipleFallbackHandlers_UsesCorrectHandlerByJobId()
        {
            // Arrange
            var services = new ServiceCollection();
            MessageHandlerCollection collection1 = services.AddEventHubsMessageRouting();
            collection1.JobId = Guid.NewGuid().ToString();

            MessageHandlerCollection collection2 = services.AddEventHubsMessageRouting();
            collection2.JobId = Guid.NewGuid().ToString();

            var handler1 = new PassThruFallbackMessageHandler<AzureEventHubsMessageContext>();
            var handler2 = new PassThruFallbackMessageHandler<AzureEventHubsMessageContext>();
            collection1.WithFallbackMessageHandler<PassThruFallbackMessageHandler<AzureEventHubsMessageContext>, AzureEventHubsMessageContext>(provider => handler1);
            collection2.WithFallbackMessageHandler<PassThruFallbackMessageHandler<AzureEventHubsMessageContext>, AzureEventHubsMessageContext>(provider => handler2);

            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureEventHubsMessageRouter>();

            Order order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "name", "consumer-group", collection1.JobId);
            MessageCorrelationInfo correlationInfo = eventData.GetCorrelationInfo();

            // Act
            await router.RouteMessageAsync(eventData, context, correlationInfo, CancellationToken.None);

            // Assert
            Assert.True(handler1.IsProcessed);
            Assert.False(handler2.IsProcessed);
        }

        [Fact]
        public async Task AddWithFactory_WithMultipleMessageHandlers_ChoosesRightViaEventData()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredMessageHandler = new IgnoreEventHubsMessageHandler<Customer>();
            var correctMessageHandler = new OrderEventHubsMessageHandler();

            // Act
            services.AddEventHubsMessageRouting()
                    .WithEventHubsMessageHandler<IgnoreEventHubsMessageHandler<Customer>, Customer>(provider => ignoredMessageHandler)
                    .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>(provider => correctMessageHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureEventHubsMessageRouter>();

            var order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name", "job-id");
            var correlation = new MessageCorrelationInfo("operation-id", "transaction-id");

            await router.RouteMessageAsync(eventData, context, correlation, CancellationToken.None);
            Assert.True(correctMessageHandler.IsProcessed);
            Assert.False(ignoredMessageHandler.IsProcessed);
        }

        [Fact]
        public async Task Add_WithoutMessageHandlers_FailsViaEventData()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEventHubsMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureEventHubsMessageRouter>();

            var order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name", "job-id");
            var correlation = new MessageCorrelationInfo("operation-id", "transaction-id");

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => router.RouteMessageAsync(eventData, context, correlation, CancellationToken.None));
        }

        [Fact]
        public async Task AddWithFactory_WithMultipleMessageHandlers_ChoosesRightViaEventBody()
        {
            // Arrange
            var services = new ServiceCollection();
            var ignoredMessageHandler = new IgnoreEventHubsMessageHandler<Customer>();
            var correctMessageHandler = new OrderEventHubsMessageHandler();

            // Act
            services.AddEventHubsMessageRouting()
                    .WithEventHubsMessageHandler<IgnoreEventHubsMessageHandler<Customer>, Customer>(provider => ignoredMessageHandler)
                    .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>(provider => correctMessageHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureEventHubsMessageRouter>();

            var order = OrderGenerator.Generate();
            string json = JsonConvert.SerializeObject(order);
            var eventData = new EventData(json);
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name", "job-id");
            var correlation = new MessageCorrelationInfo("operation-id", "transaction-id");

            await router.RouteMessageAsync(json, context, correlation, CancellationToken.None);
            Assert.True(correctMessageHandler.IsProcessed);
            Assert.False(ignoredMessageHandler.IsProcessed);
        }

        [Fact]
        public async Task Add_WithoutMessageHandlers_FailsViaEventBody()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEventHubsMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetRequiredService<IAzureEventHubsMessageRouter>();

            var order = OrderGenerator.Generate();
            string json = JsonConvert.SerializeObject(order);
            var eventData = new EventData(json);
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name", "job-id");
            var correlation = new MessageCorrelationInfo("operation-id", "transaction-id");

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => router.RouteMessageAsync(json, context, correlation, CancellationToken.None));
        }

        [Fact]
        public void Add_WithoutOptions_AddRouter()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEventHubsMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetService<IAzureEventHubsMessageRouter>();
            Assert.NotNull(router);
        }

        [Fact]
        public void Add_WithImplementationFactory_AddRouter()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEventHubsMessageRouting(provider => new TestAzureEventHubsMessageRouter(provider));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetService<IAzureEventHubsMessageRouter>();
            Assert.NotNull(router);
            Assert.IsType<TestAzureEventHubsMessageRouter>(router);
            Assert.NotNull(provider.GetService<IMessageRouter>());
        }

        [Fact]
        public void Add_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddEventHubsMessageRouting<AzureEventHubsMessageRouter>(implementationFactory: null));
        }

        [Fact]
        public void Add_WithOptionsWithImplementationFactory_AddRouter()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEventHubsMessageRouting((provider, options) => new TestAzureEventHubsMessageRouter(provider), options => { });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetService<IAzureEventHubsMessageRouter>();
            Assert.NotNull(router);
            Assert.IsType<TestAzureEventHubsMessageRouter>(router);
            Assert.NotNull(provider.GetService<IMessageRouter>());
        }

        [Fact]
        public void Add_WithOptionsWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddEventHubsMessageRouting<AzureEventHubsMessageRouter>(
                    implementationFactory: null, 
                    configureOptions: options => { }));
        }
    }
#endif
}
