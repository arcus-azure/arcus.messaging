using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using GuardNet;

namespace Arcus.Messaging.Pumps.EventHubs.Configuration
{
    /// <summary>
    /// Represents the Azure EventHubs configuration that is used to setup the interaction with Azure EventHubs for the <see cref="AzureEventHubsMessagePump"/>.
    /// </summary>
    internal class AzureEventHubsMessagePumpConfig
    {
        private readonly ISecretProvider _secretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessagePumpConfig" /> class.
        /// </summary>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="eventHubsConnectionStringSecretName">
        ///     The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<paramref name="secretProvider"/>) implementation.
        /// </param>
        /// <param name="blobContainerName">
        ///     The name of the Azure Blob storage container in the storage account to reference where the event checkpoints will be stored and the load balanced.
        /// </param>
        /// <param name="storageAccountConnectionStringSecretName">
        ///     The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<paramref name="secretProvider"/>) implementation.
        /// </param>
        /// <param name="secretProvider">
        ///     The application's secret provider provider to retrieve the connection strings from both <paramref name="eventHubsConnectionStringSecretName"/> and <paramref name="storageAccountConnectionStringSecretName"/>.
        /// </param>
        /// <param name="options">The additional options to influence the behavior of the message pump.</param>
        ///   /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsName"/>, the <paramref name="eventHubsConnectionStringSecretName"/>, the <paramref name="blobContainerName"/>,
        ///     or the <paramref name="storageAccountConnectionStringSecretName"/> is blank.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or the <paramref name="options"/> is <c>null</c>.</exception>
        internal AzureEventHubsMessagePumpConfig(
            string eventHubsName, 
            string eventHubsConnectionStringSecretName, 
            string blobContainerName, 
            string storageAccountConnectionStringSecretName,
            ISecretProvider secretProvider,
            AzureEventHubsMessagePumpOptions options)
        {
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank Azure EventHubs name where the events will be sent to when adding an Azure EvenHubs message pump");
            Guard.NotNullOrWhitespace(eventHubsConnectionStringSecretName, nameof(eventHubsConnectionStringSecretName), "Requires a non-blank secret name to retrieve the connection string to the Azure EventHubs where the message pump will retrieve its event messages");
            Guard.NotNullOrWhitespace(blobContainerName, nameof(blobContainerName), "Requires a non-blank Azure Blob storage container name to store event checkpoints and load balance the consumed event messages send to the message pump");
            Guard.NotNullOrWhitespace(storageAccountConnectionStringSecretName, nameof(storageAccountConnectionStringSecretName), "Requires a non-blank secret name to retrieve the connection string to the Azure Blob storage where the event checkpoints will be stored and events will be load balanced during the event processing of the message pump");
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires an application's service provider to retrieve registered services during the lifetime of the message pump, like Azure EventHubs message handlers and its dependencies");
            Guard.NotNull(options, nameof(options), "Requires a set of user-defined options to influence the behavior of the Azure EventHubs message pump");

            _secretProvider = secretProvider;

            EventHubsName = eventHubsName;
            EventHubsConnectionStringSecretName = eventHubsConnectionStringSecretName;
            BlobContainerName = blobContainerName;
            StorageAccountConnectionStringSecretName = storageAccountConnectionStringSecretName;
            Options = options;
        }

        /// <summary>
        /// Gets the name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.
        /// </summary>
        public string EventHubsName { get; }

        /// <summary>
        /// Gets the name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<see cref="ISecretProvider" />) implementation
        /// </summary>
        public string EventHubsConnectionStringSecretName { get; }

        /// <summary>
        /// Gets the name of the Azure Blob storage container in the storage account to reference where the event checkpoints will be stored and the load balanced.
        /// </summary>
        public string BlobContainerName { get; }

        /// <summary>
        /// Gets the name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<see cref="ISecretProvider" />) implementation.
        /// </summary>
        public string StorageAccountConnectionStringSecretName { get; }

        /// <summary>
        /// Gets the additional options to influence the behavior of the message pump.
        /// </summary>
        public AzureEventHubsMessagePumpOptions Options { get; }

        /// <summary>
        /// Creates an <see cref="EventProcessorClient"/> based on the provided information in this configuration instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no Arcus secret store is configured in the application or when the secret store was not configured correctly.
        /// </exception>
        internal async Task<EventProcessorClient> CreateEventProcessorClientAsync()
        {
            string storageAccountConnectionString = await GetConnectionStringFromSecretAsync(StorageAccountConnectionStringSecretName, "Azure Blob storage account");
            var storageClient = new BlobContainerClient(storageAccountConnectionString, BlobContainerName);

            string eventHubsConnectionString = await GetConnectionStringFromSecretAsync(EventHubsConnectionStringSecretName, "Azure EventHubs");
            var eventProcessor = new EventProcessorClient(storageClient, Options.ConsumerGroup, eventHubsConnectionString, EventHubsName);

            return eventProcessor;
        }

        private async Task<string> GetConnectionStringFromSecretAsync(string connectionStringSecretName, string connectionStringType)
        {
            Task<string> getConnectionStringTask = _secretProvider.GetRawSecretAsync(connectionStringSecretName);
            if (getConnectionStringTask is null)
            {
                throw new InvalidOperationException(
                    $"Cannot retrieve {connectionStringType} connection string via calling the {nameof(ISecretProvider)} because the operation resulted in 'null'");
            }

            return await getConnectionStringTask;
        }
    }
}
