using System;
using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a configuration section that provides information on Azure EventHubs used during integration testing.
    /// </summary>
    public class EventHubsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        /// <param name="eventHubsName">The name of the Azure EventHubs instance used in the integration tests.</param>
        /// <param name="connectionString">The connection string to connect to the <paramref name="eventHubsName"/> on Azure.</param>
        /// <param name="storageConnectionString">
        ///     The connection string used to connect to the related Azure Blob storage instance where checkpoints are stored and load balancing done.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsName"/>, <paramref name="connectionString"/>, or the <paramref name="storageConnectionString"/> is blank.
        /// </exception>
        public EventHubsConfig(string eventHubsName, string connectionString, string storageConnectionString)
        {
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank name for the Azure EventHubs instance used in the integration tests");
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank connection string to connect to the Azure EventHubs instance");
            Guard.NotNullOrWhitespace(storageConnectionString, nameof(storageConnectionString), "Requires a non-blank connection string to connect to the related Azure Blob storage instance for Azure EventHubs");

            EventHubsName = eventHubsName;
            EventHubsConnectionString = connectionString;
            StorageConnectionString = storageConnectionString;
        }

        /// <summary>
        /// Gets the name of the Azure EventHubs instance prepared for the integration tests.
        /// </summary>
        public string EventHubsName { get; }

        /// <summary>
        /// Gets the connection string to connect to the <see cref="EventHubsName"/> Azure EventHubs instance.
        /// </summary>
        public string EventHubsConnectionString { get; }

        /// <summary>
        /// Gets the connection string to connect to the related Azure Blob storage instance that is prepared for the integration tests.
        /// </summary>
        public string StorageConnectionString { get; }
    }
}
