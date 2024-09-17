using System;
using Azure.Storage.Blobs;
using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a configuration section that provides information on Azure EventHubs used during integration testing.
    /// </summary>
    public class EventHubsConfig
    {
        private readonly string _eventHubsName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        /// <param name="eventHubsName">The name of the Azure EventHubs instance used in the slf-contained integration tests.</param>
        /// <param name="connectionString">The connection string to connect to the <paramref name="eventHubsName"/> on Azure.</param>
        /// <param name="storageConnectionString">
        ///     The connection string used to connect to the related Azure Blob storage instance where checkpoints are stored and load balancing done.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsName"/>, <paramref name="connectionString"/>, or the <paramref name="storageConnectionString"/> is blank.
        /// </exception>
        public EventHubsConfig(
            string eventHubsName, 
            string connectionString, 
            string storageConnectionString)
        {
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank name for the Azure EventHubs instance used in the self-contained integration tests");
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank connection string to connect to the Azure EventHubs instance");
            Guard.NotNullOrWhitespace(storageConnectionString, nameof(storageConnectionString), "Requires a non-blank connection string to connect to the related Azure Blob storage instance for Azure EventHubs");

            _eventHubsName = eventHubsName;

            EventHubsConnectionString = connectionString;
            StorageConnectionString = storageConnectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        public EventHubsConfig(
            ServicePrincipal servicePrincipal,
            string eventHubsName,
            string storageAccountName)
        {
            Name = eventHubsName;
            Storage = new BlobStorageConfig(servicePrincipal, storageAccountName);
        }

        public string Name { get; }

        public BlobStorageConfig Storage { get; }



        /// <summary>
        /// Gets the connection string to connect to the Azure EventHubs instance.
        /// </summary>
        public string EventHubsConnectionString { get; }

        /// <summary>
        /// Gets the connection string to connect to the related Azure Blob storage instance that is prepared for the integration tests.
        /// </summary>
        public string StorageConnectionString { get; }

        /// <summary>
        /// Gets the configured Azure EventHubs name used during the integration tests.
        /// </summary>
        /// <param name="type">The type of test that request the name of the Azure EventHubs instance.</param>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="type"/> is outside the bounds of the enumeration.</exception>
        public string GetEventHubsName(IntegrationTestType type)
        {
            switch (type)
            {
                case IntegrationTestType.SelfContained: return _eventHubsName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown integration test type");
            }
        }
    }

    public class BlobStorageConfig
    {
        private readonly ServicePrincipal _servicePrincipal;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStorageConfig" /> class.
        /// </summary>
        public BlobStorageConfig(ServicePrincipal servicePrincipal, string name)
        {
            _servicePrincipal = servicePrincipal;
            Name = name;
        }

        public string Name { get; }
    }
}
