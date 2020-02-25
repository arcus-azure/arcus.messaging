using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
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
                services.AddServiceBusTopicMessagePump<EmptyMessagePump>(
                    "subscription name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<EmptyMessagePump>(messagePump);

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
                services.AddServiceBusTopicMessagePump<EmptyMessagePump>(
                    "topic name", "subscription name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<EmptyMessagePump>(messagePump);

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
                services.AddServiceBusQueueMessagePump<EmptyMessagePump>(
                    "queue name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<EmptyMessagePump>(messagePump);

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
                services.AddServiceBusQueueMessagePump<EmptyMessagePump>("secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<EmptyMessagePump>(messagePump);

            var settings = provider.GetService<AzureServiceBusMessagePumpSettings>();
            await settings.GetConnectionStringAsync();
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }
    }
}
