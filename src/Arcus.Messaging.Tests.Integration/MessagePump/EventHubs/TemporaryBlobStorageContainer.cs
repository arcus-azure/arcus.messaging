using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.MessagePump.EventHubs
{
    /// <summary>
    /// Represents a temporary disposable fixture that sets up an Azure Blob storage container.
    /// </summary>
    public class TemporaryBlobStorageContainer : IAsyncDisposable
    {
        private readonly BlobServiceClient _client;
        private readonly ILogger _logger;

        private TemporaryBlobStorageContainer(BlobServiceClient client, string containerName, ILogger logger)
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure Blob storage client to create an temporary container");
            Guard.NotNullOrWhitespace(containerName, nameof(containerName), "Requires a non-blank Azure Blob storage container name");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic trace messages during the lifetime of the temporary Azure Blob storage container");

            _client = client;
            _logger = logger;

            ContainerName = containerName;
            ContainerUri = client.Uri.OriginalString;
        }

        /// <summary>
        /// Gets the container name used during this temporary Azure Blob storage container.
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// Gets the container primary endpoint used during this temporary Azure Blob storage container.
        /// </summary>
        public string ContainerUri { get; }

        /// <summary>
        /// Creates an <see cref="TemporaryBlobStorageContainer"/> instance that creates an Azure Blob container on the Azure storage account
        /// that is accessed via the given <paramref name="storageAccountConnectionString"/>.
        /// </summary>
        /// <param name="storageAccountConnectionString">The Azure storage account connection string on which a temporary Azure Blob storage container should be created.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the lifetime of the temporary Azure blob storage container.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="storageAccountConnectionString"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobStorageContainer> CreateAsync(string storageAccountConnectionString, ILogger logger)
        {
            Guard.NotNullOrWhitespace(storageAccountConnectionString, nameof(storageAccountConnectionString), "Requires a non-blank connection string to access the Azure storage account");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic trace messages during the lifetime of the temporary Azure Blob storage container");

            string containerName = $"eventhubs-{Guid.NewGuid()}";
            return await CreateAsync(storageAccountConnectionString, containerName, logger);
        }

         /// <summary>
        /// Creates an <see cref="TemporaryBlobStorageContainer"/> instance that creates an Azure Blob container on the Azure storage account
        /// that is accessed via the given <paramref name="storageAccountConnectionString"/>.
        /// </summary>
        /// <param name="storageAccountConnectionString">The Azure storage account connection string on which a temporary Azure Blob storage container should be created.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the lifetime of the temporary Azure blob storage container.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="storageAccountConnectionString"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobStorageContainer> CreateAsync(string storageAccountConnectionString, string containerName, ILogger logger)
        {
            Guard.NotNullOrWhitespace(storageAccountConnectionString, nameof(storageAccountConnectionString), "Requires a non-blank connection string to access the Azure storage account");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic trace messages during the lifetime of the temporary Azure Blob storage container");

            var blobClient = new BlobServiceClient(storageAccountConnectionString);
            
            logger.LogTrace("Add Azure Blob storage container '{ContainerName}'", containerName);
            await blobClient.CreateBlobContainerAsync(containerName, PublicAccessType.Blob);

            return new TemporaryBlobStorageContainer(blobClient, containerName, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("Remove Azure Blob storage container '{ContainerName}'", ContainerName);
            await _client.DeleteBlobContainerAsync(ContainerName);
        }
    }
}
