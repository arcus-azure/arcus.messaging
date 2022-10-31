using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Unit.EventHubs.Fixture;
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

namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs
{
#if NET6_0
    public class AzureEventHubsMessageRouterTests
    {
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
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name");
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
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name");
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
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name");
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
            AzureEventHubsMessageContext context = eventData.GetMessageContext("namespace", "consumergroup", "name");
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
