using System;
using Arcus.Messaging.Tests.Unit.Fixture;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs
{
    public class AzureClientFactoryBuilderExtensionsTests
    {
        private const string ExampleEventHubsConnectionStringWithEntityPath = "Endpoint=sb://arcus-testing-messaging.servicebus.windows.net/;SharedAccessKeyName=KeyName;SharedAccessKey=123;EntityPath=eventhubs-name",
                             ExampleEventHubsConnectionStringWithoutEntityPath = "Endpoint=sb://arcus-testing-messaging.servicebus.windows.net/;SharedAccessKeyName=KeyName;SharedAccessKey=123";

        [Fact]
        public void AddEventHubProducerClient_WithEntityConnectionStringInSecretStore_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";
            services.AddSecretStore(stores => stores.AddProvider(new StaticInMemorySecretProvider(secretName, ExampleEventHubsConnectionStringWithEntityPath)));

            // Act
            services.AddAzureClients(clients => clients.AddEventHubProducerClient(secretName));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventHubProducerClient>>();
            Assert.NotNull(factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventHubProducerClient_WithNamespaceConnectionStringInSecretStore_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";
            services.AddSecretStore(stores => stores.AddProvider(new StaticInMemorySecretProvider(secretName, ExampleEventHubsConnectionStringWithoutEntityPath)));

            // Act
            services.AddAzureClients(clients => clients.AddEventHubProducerClient(secretName, "eventhubs-name"));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventHubProducerClient>>();
            Assert.NotNull(factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventHubProducerClientFromEntity_WithoutSyncSecretProvider_FallbackOnAsyncSecretProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";
            services.AddSecretStore(stores => stores.AddProvider(new AsyncInMemorySecretProvider(secretName, ExampleEventHubsConnectionStringWithoutEntityPath)));

            // Act
            services.AddAzureClients(clients => clients.AddEventHubProducerClient(secretName, "eventhubs-name"));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventHubProducerClient>>();
            Assert.NotNull(factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventHubProducerClientFromEntity_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAzureClients(clients => clients.AddEventHubProducerClient("MySecret", "<eventhubs-name>"));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventHubProducerClient>>();
            Assert.Throws<InvalidOperationException>(() => factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventHubProducerClientFromNamespace_WithoutSyncSecretProvider_FallbackOnAsyncSecretProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var secretName = "MyConnectionString";
            services.AddSecretStore(stores => stores.AddProvider(new AsyncInMemorySecretProvider(secretName, ExampleEventHubsConnectionStringWithEntityPath)));

            // Act
            services.AddAzureClients(clients => clients.AddEventHubProducerClient(secretName));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventHubProducerClient>>();
            Assert.NotNull(factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventHubProducerClientFromNamespace_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAzureClients(clients => clients.AddEventHubProducerClient("MySecret"));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventHubProducerClient>>();
            Assert.Throws<InvalidOperationException>(() => factory.CreateClient("Default"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventHubProducerClient_WithoutEntityConnectionString_Fails(string connectionStringSecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(clients => clients.AddEventHubProducerClient(connectionStringSecretName: connectionStringSecretName)));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventHubProducerClient_WithoutEntityConnectionStringWithOptions_Fails(string connectionStringSecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(clients => clients.AddEventHubProducerClient(connectionStringSecretName: connectionStringSecretName, options => { })));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventHubProducerClient_WithoutNamespaceConnectionString_Fails(string connectionStringSecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(clients => clients.AddEventHubProducerClient(connectionStringSecretName: connectionStringSecretName, "<eventhubs-name>")));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventHubProducerClient_WithoutNamespaceConnectionStringWithOptions_Fails(string connectionStringSecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(clients => clients.AddEventHubProducerClient(connectionStringSecretName: connectionStringSecretName, "<eventhubs-name>", options => { })));
        }
    }
}
