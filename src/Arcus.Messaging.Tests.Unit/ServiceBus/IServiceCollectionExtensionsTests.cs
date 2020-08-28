using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.Extensions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Security.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public async Task AddServiceBusTopicMessagePump_WithSubscriptionNameIndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            IServiceCollection result = 
                services.AddServiceBusTopicMessagePump(
                    "topic name", "subscription name", "secret name", options => options.AutoComplete = true);
            
            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            await Assert.ThrowsAnyAsync<Exception>(() => messagePump.StartAsync(CancellationToken.None));
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }

        [Fact]
        public async Task AddServiceBusTopicMessagePump_WithTopicNameAndSubscriptionNameIndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            IServiceCollection result = 
                services.AddServiceBusTopicMessagePump(
                    "topic name", "subscription name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            await Assert.ThrowsAnyAsync<Exception>(() => messagePump.StartAsync(CancellationToken.None));
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }

        [Fact]
        public async Task AddServiceBusQueueMessagePump_IndirectSecretProviderWithQueueName_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            IServiceCollection result =
                services.AddServiceBusQueueMessagePump(
                    "queue name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            try
            {
                await messagePump.StartAsync(CancellationToken.None);
            }
            finally
            {
                spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
            }
        }

        [Fact]
        public async Task AddServiceBusQueueMessagePump_IndirectSecretProviderWithoutQueueName_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            IServiceCollection result =
                services.AddServiceBusQueueMessagePump("secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            try
            {
                await messagePump.StartAsync(CancellationToken.None);
            }
            finally
            {
                spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
            }
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandler_WithValidType_RegistersInterface()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.WithServiceBusFallbackMessageHandler<PassThruServiceBusFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var messageHandler = provider.GetRequiredService<IAzureServiceBusFallbackMessageHandler>();

            Assert.IsType<PassThruServiceBusFallbackMessageHandler>(messageHandler);
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandler_WithValidImplementationFunction_RegistersInterface()
        {
            // Arrange
            var services = new ServiceCollection();
            var expected = new PassThruServiceBusFallbackMessageHandler();

            // Act
            services.WithServiceBusFallbackMessageHandler(serviceProvider => expected);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var actual = provider.GetRequiredService<IAzureServiceBusFallbackMessageHandler>();

            Assert.Same(expected, actual);
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandlerType_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((IServiceCollection)null).WithServiceBusFallbackMessageHandler<PassThruServiceBusFallbackMessageHandler>());
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandlerImplementationFunction_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((IServiceCollection)null).WithServiceBusFallbackMessageHandler(serviceProvider => new PassThruServiceBusFallbackMessageHandler()));
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandlerImplementationFunction_WithoutImplementationFunction_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusFallbackMessageHandler(createImplementation: (Func<IServiceProvider, PassThruServiceBusFallbackMessageHandler>)null));
        }
    }
}
