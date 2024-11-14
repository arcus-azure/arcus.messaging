using System;
using System.Threading.Tasks;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class TemporaryServiceBusEntity : IAsyncDisposable
    {
        private readonly ServiceBusEntityType _entityType;
        private readonly ServiceBusNamespaceResource _ns;
        private readonly ILogger _logger;

        private TemporaryServiceBusEntity(ServiceBusEntityType entityType, string entityName, ServiceBusNamespaceResource @namespace, ILogger logger)
        {
            _entityType = entityType;
            _ns = @namespace;
            _logger = logger;
            
            EntityName = entityName;
        }

        public string EntityName { get; }

        public static async Task<TemporaryServiceBusEntity> CreateAsync(ServiceBusEntityType entityType, string entityName, ServiceBusConfig serviceBus, ILogger logger)
        {
            var armClient = new ArmClient(serviceBus.ServicePrincipal.GetCredential());
            ServiceBusNamespaceResource serviceBusNamespace = armClient.GetServiceBusNamespaceResource(serviceBus.ResourceId);

            switch (entityType)
            {
                case ServiceBusEntityType.Queue:
                    logger.LogTrace("[Test] create Service bus queue '{EntityName}'", entityName);
                    await serviceBusNamespace.GetServiceBusQueues()
                                             .CreateOrUpdateAsync(WaitUntil.Completed, entityName, new ServiceBusQueueData());
                    break;

                case ServiceBusEntityType.Topic:
                    logger.LogTrace("[Test] create Service bus topic '{EntityName}'", entityName);
                    await serviceBusNamespace.GetServiceBusTopics()
                                             .CreateOrUpdateAsync(WaitUntil.Completed, entityName, new ServiceBusTopicData());
                    break;

                case ServiceBusEntityType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service bus entity type");
            }

            return new TemporaryServiceBusEntity(entityType, entityName, serviceBusNamespace, logger);
        }

        public async ValueTask DisposeAsync()
        {
            switch (_entityType)
            {
                case ServiceBusEntityType.Queue:
                    _logger.LogTrace("[Test] delete Service bus queue '{EntityName}''", EntityName);
                    ServiceBusQueueResource queue = await _ns.GetServiceBusQueueAsync(EntityName);
                    await queue.DeleteAsync(WaitUntil.Started);
                    break;
                
                case ServiceBusEntityType.Topic:
                    _logger.LogTrace("[Test] delete Service bus topic '{EntityName}'", EntityName);
                    ServiceBusTopicResource topic = await _ns.GetServiceBusTopicAsync(EntityName);
                    await topic.DeleteAsync(WaitUntil.Started);
                    break;
                
                case ServiceBusEntityType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
