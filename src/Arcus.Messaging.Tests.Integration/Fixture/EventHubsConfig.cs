using System;
using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a configuration section that provides information on Azure EventHubs used during integration testing.
    /// </summary>
    public class EventHubsConfig
    {
        private readonly string _selfContainedEventHubsName, _dockerEventHubsName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        /// <param name="selfContainedEventHubsName">The name of the Azure EventHubs instance used in the slf-contained integration tests.</param>
        /// <param name="dockerEventHubsName">The name of the Azure EventHubs instance used in the docker integration tests.</param>
        /// <param name="connectionString">The connection string to connect to the <paramref name="selfContainedEventHubsName"/> on Azure.</param>
        /// <param name="storageConnectionString">
        ///     The connection string used to connect to the related Azure Blob storage instance where checkpoints are stored and load balancing done.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="selfContainedEventHubsName"/>, <paramref name="connectionString"/>, or the <paramref name="storageConnectionString"/> is blank.
        /// </exception>
        public EventHubsConfig(
            string selfContainedEventHubsName, 
            string dockerEventHubsName,
            string connectionString, 
            string storageConnectionString)
        {
            Guard.NotNullOrWhitespace(selfContainedEventHubsName, nameof(selfContainedEventHubsName), "Requires a non-blank name for the Azure EventHubs instance used in the self-contained integration tests");
            Guard.NotNullOrWhitespace(dockerEventHubsName, nameof(dockerEventHubsName), "Requires a non-blank name for the Azure EventHubs instance used in the docker integration tests");
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank connection string to connect to the Azure EventHubs instance");
            Guard.NotNullOrWhitespace(storageConnectionString, nameof(storageConnectionString), "Requires a non-blank connection string to connect to the related Azure Blob storage instance for Azure EventHubs");

            _selfContainedEventHubsName = selfContainedEventHubsName;
            _dockerEventHubsName = dockerEventHubsName;

            EventHubsConnectionString = connectionString;
            StorageConnectionString = storageConnectionString;
        }

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
                case IntegrationTestType.SelfContained: return _selfContainedEventHubsName;
                case IntegrationTestType.Docker: return _dockerEventHubsName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown integration test type");
            }
        }
    }
}
