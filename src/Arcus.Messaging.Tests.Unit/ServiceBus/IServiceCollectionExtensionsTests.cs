using System;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Security.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            services.AddSingleton(serviceProvider => Mock.Of<ILogger>());

            // Act
            IServiceCollection result = 
                services.AddServiceBusTopicMessagePump(
                    "topic name", "subscription name", "secret name", options => options.AutoComplete = true);
            
            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            var settings = provider.GetService<AzureServiceBusMessagePumpSettings>();
            await settings.GetConnectionStringAsync();
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
            services.AddSingleton(serviceProvider => Mock.Of<ILogger>());

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

            var settings = provider.GetService<AzureServiceBusMessagePumpSettings>();
            await settings.GetConnectionStringAsync();
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
            services.AddSingleton(serviceProvider => Mock.Of<ILogger>());

            // Act
            IServiceCollection result =
                services.AddServiceBusQueueMessagePump(
                    "queue name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            var settings = provider.GetService<AzureServiceBusMessagePumpSettings>();
            await settings.GetConnectionStringAsync();
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }

        [Fact]
        public async Task AddServiceBusQueueMessagePump_IndirectSecretProviderWithoutQueueName_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddSingleton(serviceProvider => Mock.Of<ILogger>());

            // Act
            IServiceCollection result =
                services.AddServiceBusQueueMessagePump("secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            var settings = provider.GetService<AzureServiceBusMessagePumpSettings>();
            await settings.GetConnectionStringAsync();
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }

        [Fact]
        public async Task AddServiceBusTopicMessagePumpWithPrefix_WithSubscriptionPrefix_SubscriptionNameIsAssembledWithJobId()
        {
            // Arrange
            string subscriptionPrefix = $"subscription-{Guid.NewGuid()}";

            var services =  new ServiceCollection();
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddSingleton(serviceProvider => Mock.Of<ILogger>());

            // Act
            IServiceCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix(subscriptionPrefix, config => "ignored connection string");

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            var settings = provider.GetService<AzureServiceBusMessagePumpSettings>();
            Assert.StartsWith(subscriptionPrefix, settings.SubscriptionName);
            Assert.True(
                subscriptionPrefix.Length < settings.SubscriptionName.Length, 
                "subscription prefix's length should be less than subscription name");
        }

        [Fact]
        public async Task AddServiceBusTopicMessagePumpWithPrefix_IndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var spySecretProvider = new Mock<ISecretProvider>();
            var services =  new ServiceCollection();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddSingleton(serviceProvider => Mock.Of<ILogger>());

            // Act
            IServiceCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix($"subscription-prefix-{Guid.NewGuid()}", "secret name");

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            var settings = provider.GetService<AzureServiceBusMessagePumpSettings>();
            await settings.GetConnectionStringAsync();
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }
    }
}
