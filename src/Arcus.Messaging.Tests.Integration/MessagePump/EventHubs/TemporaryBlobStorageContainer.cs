using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.MessagePump.EventHubs
{
    public class TemporaryBlobStorageContainer : IAsyncDisposable
    {
        private readonly BlobServiceClient _client;
        private readonly ILogger _logger;

        private TemporaryBlobStorageContainer(BlobServiceClient client, string containerName, ILogger logger)
        {
            _client = client;
            _logger = logger;

            ContainerName = containerName;
        }

        public string ContainerName { get; }

        public static async Task<TemporaryBlobStorageContainer> CreateAsync(string storageAccountConnectionString, ILogger logger)
        {
            string containerName = $"eventhubs-{Guid.NewGuid()}";
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
