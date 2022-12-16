using System;
using Arcus.Messaging.Tests.Unit.Fixture;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class AzureClientFactoryBuilderExtensionsTests
    {
        private const string ExampleServiceBusConnectionString = "Endpoint=sb://arcus-testing-messaging.servicebus.windows.net/;SharedAccessKeyName=KeyName;SharedAccessKey=123";

        [Fact]
        public void AddServiceBusClient_WithConnectionStringSecretInSecretStore_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";
            services.AddSecretStore(stores => stores.AddProvider(new StaticInMemorySecretProvider(secretName, ExampleServiceBusConnectionString)));

            // Act
            services.AddAzureClients(clients => clients.AddServiceBusClient(secretName));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
            Assert.NotNull(client.CreateClient("Default"));
        }

        [Fact]
        public void AddServiceBusClient_WithConnectionStringSecretInSecretStoreWithOptions_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";
            services.AddSecretStore(stores => stores.AddProvider(new StaticInMemorySecretProvider(secretName, ExampleServiceBusConnectionString)));

            // Act
            services.AddAzureClients(clients => clients.AddServiceBusClient(secretName, configureOptions: options => { }));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
            Assert.NotNull(client.CreateClient("Default"));
        }

        [Fact]
        public void AddServiceBusClient_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";

            // Act
            services.AddAzureClients(clients => clients.AddServiceBusClient(secretName));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
            Assert.Throws<InvalidOperationException>(() => client.CreateClient("Default"));
        }

        [Fact]
        public void AddServiceBusClient_WithoutSecretStoreWithOptions_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";

            // Act
            services.AddAzureClients(clients => clients.AddServiceBusClient(secretName, configureOptions: options => { }));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
            Assert.Throws<InvalidOperationException>(() => client.CreateClient("Default"));
        }

        [Fact]
        public void AddServiceBusClient_WithoutConnectionStringSecret_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(clients => clients.AddServiceBusClient(connectionStringSecretName: null)));
        }

        [Fact]
        public void AddServiceBusClient_WithoutConnectionStringSecretWithOptions_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(clients => clients.AddServiceBusClient(connectionStringSecretName: null, configureOptions: options => { })));
        }
    }
}
