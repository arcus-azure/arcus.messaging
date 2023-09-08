using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Pumps.EventHubs;
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
        private static readonly Faker Bogus = new Faker();

        [Fact]
        public void AddWithoutOptions_WithSecretStore_Succeeds()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
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
        public void AddWithoutOptions_WithCustomJobId_Succeeds()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
            string jobId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IConfiguration>())
                    .AddLogging()
                    .AddSecretStore(stores => stores.AddInMemory());

            // Act
            EventHubsMessageHandlerCollection collection = 
                services.AddEventHubsMessagePump(
                    eventHubsName, eventHubsConnectionStringSecretName, blobStorageContainerName, storageAccountConnectionStringSecretName, 
                    options => options.JobId = jobId);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var host = provider.GetService<IHostedService>();
            Assert.NotNull(host);
            Assert.Equal(jobId, collection.JobId);
        }

        [Fact]
        public void AddWithoutOptions_WithoutSecretStore_Fails()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
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
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
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
            string eventHubsName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
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
            string eventHubsName = Bogus.Lorem.Word();
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
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
            string eventHubsName = Bogus.Lorem.Word();
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
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
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithOptions_WithoutEventHubsConnectionStringSecretName_Fails(string eventHubsConnectionStringSecretName)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithOptions_WithoutBlobStorageContainerName_Fails(string blobStorageContainerName)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string storageAccountConnectionStringSecretName = Bogus.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddWithOptions_WithoutStorageAccountConnectionStringSecretName_Fails(string storageAccountConnectionStringSecretName)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string eventHubsConnectionStringSecretName = Bogus.Lorem.Word();
            string blobStorageContainerName = Bogus.Lorem.Word();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePump(
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobStorageContainerName,
                storageAccountConnectionStringSecretName,
                configureOptions: _ => { }));
        }

        [Fact]
        public void AddUsingManagedIdentityWithoutClientIdAndOptions_WithValidArguments_Succeeds()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationManager());

            // Act
            services.AddEventHubsMessagePumpUsingManagedIdentity(eventHubsName, fullyQualifiedNamespace, blobContainerUri);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(
                Assert.IsType<AzureEventHubsMessagePump>(
                    Assert.Single(provider.GetServices<IHostedService>())));
        }

        [Fact]
        public void AddUsingManagedIdentityWithClientIdAndWithoutOptions_WithValidArguments_Succeeds()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationManager());

            // Act
            services.AddEventHubsMessagePumpUsingManagedIdentity(eventHubsName, fullyQualifiedNamespace, blobContainerUri, clientId);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(
                Assert.IsType<AzureEventHubsMessagePump>(
                    Assert.Single(provider.GetServices<IHostedService>())));
        }

        [Fact]
        public void AddUsingManagedIdentityWithoutClientIdAndWithOptions_WithValidArguments_Succeeds()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationManager());
            string jobId = null;

            // Act
            EventHubsMessageHandlerCollection collection = 
                services.AddEventHubsMessagePumpUsingManagedIdentity(eventHubsName, fullyQualifiedNamespace, blobContainerUri, opt => jobId = opt.JobId);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var pump = Assert.IsType<AzureEventHubsMessagePump>(
                Assert.Single(provider.GetServices<IHostedService>()));
            
            Assert.NotNull(pump);
            Assert.NotNull(jobId);
            Assert.Equal(collection.JobId, pump.JobId);
            Assert.Equal(jobId, pump.JobId);
        }

        [Fact]
        public void AddUsingManagedIdentityWithClientIdAndOptions_WithValidArguments_Succeeds()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationManager());
            string jobId = null;

            // Act
            EventHubsMessageHandlerCollection collection = 
                services.AddEventHubsMessagePumpUsingManagedIdentity(eventHubsName, fullyQualifiedNamespace, blobContainerUri, clientId, opt => jobId = opt.JobId);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var pump = Assert.IsType<AzureEventHubsMessagePump>(
                Assert.Single(provider.GetServices<IHostedService>()));
            
            Assert.NotNull(pump);
            Assert.NotNull(jobId);
            Assert.Equal(collection.JobId, pump.JobId);
            Assert.Equal(jobId, pump.JobId);
        }

        [Fact]
        public void AddUsingManagedIdentityWithClientIdAndOptions_WithCustomJobId_Succeeds()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Word();
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationManager());
            string jobId = Bogus.Random.Guid().ToString();

            // Act
            EventHubsMessageHandlerCollection collection = 
                services.AddEventHubsMessagePumpUsingManagedIdentity(eventHubsName, fullyQualifiedNamespace, blobContainerUri, clientId, opt => opt.JobId = jobId);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var pump = Assert.IsType<AzureEventHubsMessagePump>(
                Assert.Single(provider.GetServices<IHostedService>()));
            
            Assert.NotNull(pump);
            Assert.NotNull(jobId);
            Assert.Equal(collection.JobId, pump.JobId);
            Assert.Equal(jobId, pump.JobId);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithoutClientIdAndOptions_WithoutEventHubsName_Fails(string eventHubsName)
        {
            // Arrange
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithClientIdAndWithoutOptions_WithoutEventHubsName_Fails(string eventHubsName)
        {
            // Arrange
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId));
        }

          [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithoutClientIdAndWithOptions_WithoutEventHubsName_Fails(string eventHubsName)
        {
            // Arrange
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithClientIdAndOptions_WithoutEventHubsName_Fails(string eventHubsName)
        {
            // Arrange
            string fullyQualifiedNamespace = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithoutClientIdAndOptions_WithoutNamespace_Fails(string fullyQualifiedNamespace)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithClientIdAndWithoutOptions_WithoutNamespace_Fails(string fullyQualifiedNamespace)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithoutClientIdAndWithOptions_WithoutNamespace_Fails(string fullyQualifiedNamespace)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithClientIdAndOptions_WithoutNamespace_Fails(string fullyQualifiedNamespace)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string blobContainerUri = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithoutClientIdAndOptions_WithoutBlobEndpoint_Fails(string blobContainerUri)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithClientIdAndWithoutOptions_WithoutBlobEndpoint_Fails(string blobContainerUri)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithoutClientIdAndWithOptions_WithoutBlobEndpoint_Fails(string blobContainerUri)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddUsingManagedIdentityWithClientIdAndOptions_WithoutBlobEndpoint_Fails(string blobContainerUri)
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId,
                configureOptions: _ => { }));
        }

        [Fact]
        public void AddUsingManagedIdentityWithoutClientIdAndOptions_WithRelativeBlobEndpoint_Fails()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            string blobContainerUri = Bogus.Internet.UrlRootedPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<UriFormatException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri));
        }

        [Fact]
        public void AddUsingManagedIdentityWithClientIdAndWithoutOptions_WithRelativeBlobEndpoint_Fails()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            string blobContainerUri = Bogus.Internet.UrlRootedPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<UriFormatException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId));
        }

        [Fact]
        public void AddUsingManagedIdentityWithoutClientIdAndWithOptions_WithRelativeBlobEndpoint_Fails()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            string blobContainerUri = Bogus.Internet.UrlRootedPath();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<UriFormatException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                configureOptions: _ => { }));
        }

        [Fact]
        public void AddUsingManagedIdentityWithClientIdAndOptions_WithRelativeBlobEndpoint_Fails()
        {
            // Arrange
            string eventHubsName = Bogus.Lorem.Sentence();
            string fullyQualifiedNamespace = Bogus.Internet.UrlWithPath();
            string blobContainerUri = Bogus.Internet.UrlRootedPath();
            string clientId = Bogus.Random.Guid().ToString();
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<UriFormatException>(() => services.AddEventHubsMessagePumpUsingManagedIdentity(
                eventHubsName,
                fullyQualifiedNamespace,
                blobContainerUri,
                clientId,
                configureOptions: _ => { }));
        }
    }
#endif
}
