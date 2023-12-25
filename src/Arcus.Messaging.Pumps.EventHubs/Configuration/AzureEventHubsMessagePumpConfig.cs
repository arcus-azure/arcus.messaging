using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Azure.Core;
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
        private readonly Func<Task<EventProcessorClient>> _createClient;

        private AzureEventHubsMessagePumpConfig(
            Func<Task<EventProcessorClient>> createClient,
            AzureEventHubsMessagePumpOptions options)
        {
            _createClient = createClient;

            Options = options;
        }

        /// <summary>
        /// Gets the additional options to influence the behavior of the message pump.
        /// </summary>
        public AzureEventHubsMessagePumpOptions Options { get; }

        /// <summary>
        /// Creates an <see cref="AzureEventHubsMessagePumpConfig"/> instance by using a connection string to interact with Azure EventHubs.
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
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsName"/>, the <paramref name="eventHubsConnectionStringSecretName"/>, the <paramref name="blobContainerName"/>,
        ///     or the <paramref name="storageAccountConnectionStringSecretName"/> is blank.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> or the <paramref name="options"/> is <c>null</c>.</exception>
        internal static AzureEventHubsMessagePumpConfig CreateByConnectionString(
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

            return new AzureEventHubsMessagePumpConfig(async () =>
            {
                string storageAccountConnectionString = await GetConnectionStringFromSecretAsync(secretProvider, storageAccountConnectionStringSecretName, "Azure Blob storage account");
                var storageClient = new BlobContainerClient(storageAccountConnectionString, blobContainerName);

                string eventHubsConnectionString = await GetConnectionStringFromSecretAsync(secretProvider, eventHubsConnectionStringSecretName, "Azure EventHubs");
                var eventProcessor = new EventProcessorClient(storageClient, options.ConsumerGroup, eventHubsConnectionString, eventHubsName);

                return eventProcessor;
            }, options);
        }

        private static async Task<string> GetConnectionStringFromSecretAsync(
            ISecretProvider secretProvider,
            string connectionStringSecretName, 
            string connectionStringType)
        {
            Task<string> getConnectionStringTask = secretProvider.GetRawSecretAsync(connectionStringSecretName);
            if (getConnectionStringTask is null)
            {
                throw new InvalidOperationException(
                    $"Cannot retrieve {connectionStringType} connection string via calling the '{secretProvider.GetType().Name}' because the operation resulted in 'null'");
            }

            return await getConnectionStringTask;
        }

        /// <summary>
        /// Creates a <see cref="AzureEventHubsMessagePumpConfig"/> instance by using a token credential to interact with Azure EventHubs.
        /// </summary>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="fullyQualifiedNamespace">The fully qualified Event Hubs namespace to connect to.  This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="blobContainerUri">
        ///     The <see cref="BlobContainerClient.Uri" /> referencing the blob container that includes the
        ///     name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <param name="credential">The Azure identity credential to sign requests.</param>
        /// <param name="options">The additional options to influence the behavior of the message pump.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventHubsName"/> or the <paramref name="fullyQualifiedNamespace"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="credential"/> or the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="blobContainerUri"/> is not an absolute URI.</exception>
        internal static AzureEventHubsMessagePumpConfig CreateByTokenCredential(
            string eventHubsName,
            string fullyQualifiedNamespace,
            Uri blobContainerUri,
            TokenCredential credential,
            AzureEventHubsMessagePumpOptions options)
        {
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank Azure EventHubs name where the events will be sent to when adding an Azure EvenHubs message pump");
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank Azure EventHubs name where the events will be sent to when adding an Azure EvenHubs message pump");
            Guard.NotNullOrWhitespace(fullyQualifiedNamespace, nameof(fullyQualifiedNamespace), "Requires a non-blank Azure EventHubs namespace to connect to");
            Guard.For<UriFormatException>(() => !blobContainerUri.IsAbsoluteUri, "Requires a valid absolute URI endpoint for the Azure Blob container to store event checkpoints and load balance the consumed event messages send to the message pump");
            Guard.NotNull(credential, nameof(credential), "Requires a token credential to authenticate the interaction with the Azure EventHubs resource");
            Guard.NotNull(options, nameof(options), "Requires a set of user-defined options to influence the behavior of the Azure EventHubs message pump");

            return new AzureEventHubsMessagePumpConfig(() =>
            {
                var storageClient = new BlobContainerClient(blobContainerUri, credential);
                var eventProcessor = new EventProcessorClient(storageClient, options.ConsumerGroup, fullyQualifiedNamespace, eventHubsName, credential);

                return Task.FromResult(eventProcessor);
            }, options);
        }

        /// <summary>
        /// Creates an <see cref="EventProcessorClient"/> based on the provided information in this configuration instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no Arcus secret store is configured in the application or when the secret store was not configured correctly.
        /// </exception>
        internal async Task<EventProcessorClient> CreateEventProcessorClientAsync()
        {
            return await _createClient();
        }
    }
}
