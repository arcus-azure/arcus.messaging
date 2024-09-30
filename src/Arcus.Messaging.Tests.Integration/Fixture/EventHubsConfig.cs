using Arcus.Testing;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;

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
        public EventHubsConfig(
            ServicePrincipal servicePrincipal,
            string subscriptionId,
            string resourceGroupName,
            string eventHubsNamespace,
            string connectionString,
            StorageAccountConfig storageAccount)
        {
            ServicePrincipal = servicePrincipal;

            ResourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{eventHubsNamespace}");
            HostName = $"{eventHubsNamespace}.servicebus.windows.net";
            Storage = storageAccount;
            EventHubsConnectionString = connectionString;
        }

        public ServicePrincipal ServicePrincipal { get; }
        public ResourceIdentifier ResourceId { get; }
        public string HostName { get; }
        public StorageAccountConfig Storage { get; }
        public string EventHubsConnectionString { get; }

        public EventHubProducerClient GetProducerClient(string name)
        {
            return new EventHubProducerClient(HostName, name, ServicePrincipal.GetCredential());
        }

        public EventProcessorClient GetProcessorClient(string name, BlobContainerClient checkpointStore)
        {
            return new EventProcessorClient(checkpointStore, "$Default", HostName, name, ServicePrincipal.GetCredential());
        }
    }

    public static class EventHubsTestConfigExtensions
    {
        public static EventHubsConfig GetEventHubs(this TestConfig config)
        {
            return new EventHubsConfig(
                config.GetServicePrincipal(),
                config["Arcus:Infra:SubscriptionId"],
                config["Arcus:Infra:ResourceGroup:Name"],
                config["Arcus:EventHubs:Namespace"],
                config["Arcus:EventHubs:ConnectionString"],
                config.GetStorageAccount());
        }
    }
}
