using System;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs
{
#if NET6_0
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void AddWithoutOptions_WithSecretStore_Succeeds()
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IConfiguration>())
                    .AddLogging()
                    .AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddEventHubsMessagePump(eventHubsName, eventHubsConnectionStringSecretName, blobStorageContainerName, storageAccountConnectionStringSecretName);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var host = provider.GetService<IHostedService>();
            Assert.NotNull(host);
        }

        [Fact]
        public void AddWithoutOptions_WithoutSecretStore_Fails()
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IConfiguration>())
                    .AddLogging();

            // Act
            services.AddEventHubsMessagePump(eventHubsName, eventHubsConnectionStringSecretName, blobStorageContainerName, storageAccountConnectionStringSecretName);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => provider.GetService<IHostedService>());
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithoutOptions_WithoutEventHubName_Fails(string eventHubsName)
        {
            // Arrange
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithoutOptions_WithoutEventHubsConnectionStringSecretName_Fails(string eventHubsConnectionStringSecretName)
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithoutOptions_WithoutBlobStorageContainerName_Fails(string blobStorageContainerName)
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithoutOptions_WithoutStorageAccountConnectionStringSecretName_Fails(string storageAccountConnectionStringSecretName)
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName));
        }

         [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithOptions_WithoutEventHubName_Fails(string eventHubsName)
        {
            // Arrange
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithOptions_WithoutEventHubsConnectionStringSecretName_Fails(string eventHubsConnectionStringSecretName)
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithOptions_WithoutBlobStorageContainerName_Fails(string blobStorageContainerName)
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string storageAccountConnectionStringSecretName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithOptions_WithoutStorageAccountConnectionStringSecretName_Fails(string storageAccountConnectionStringSecretName)
        {
            // Arrange
            string eventHubsName = BogusGenerator.Lorem.Word();
            string eventHubsConnectionStringSecretName = BogusGenerator.Lorem.Word();
            string blobStorageContainerName = BogusGenerator.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                options => { }));
        }
    }
#endif
}
