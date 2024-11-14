using System;
using System.Threading.Tasks;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary hub on an Azure EventHubs namespace that is gets deleted when the instance gets disposed.
    /// </summary>
    public class TemporaryEventHubEntity : IAsyncDisposable
    {
        private readonly EventHubsNamespaceResource _eventHubsNamespace;
        private readonly ILogger _logger;

        private TemporaryEventHubEntity(string name, EventHubsNamespaceResource eventHubsNamespace, ILogger logger)
        {
            Name = name;
            _eventHubsNamespace = eventHubsNamespace;
            _logger = logger;
        }

        public string Name { get; }

        public static async Task<TemporaryEventHubEntity> CreateAsync(string name, EventHubsConfig config, ILogger logger)
        {
            var client = new ArmClient(config.ServicePrincipal.GetCredential());

            EventHubsNamespaceResource eventHubsNamespace = client.GetEventHubsNamespaceResource(config.ResourceId);

            logger.LogTrace("[Test] create EventHub '{HubName}'", name);
            await eventHubsNamespace.GetEventHubs()
                                    .CreateOrUpdateAsync(WaitUntil.Completed, name, new EventHubData
                                    {
                                        PartitionCount = 1, 
                                        RetentionDescription = new RetentionDescription
                                        {
                                            CleanupPolicy = CleanupPolicyRetentionDescription.Delete,
                                            RetentionTimeInHours = 1,
                                        }
                                    });

            return new TemporaryEventHubEntity(name, eventHubsNamespace, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("[Test] delete EventHub '{HubName}'", Name);
            EventHubResource hub = await _eventHubsNamespace.GetEventHubAsync(Name);
            await hub.DeleteAsync(WaitUntil.Started);

            GC.SuppressFinalize(this);
        }
    }
}
