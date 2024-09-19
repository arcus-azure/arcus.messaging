using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class TemporaryServiceBusEntity : IAsyncDisposable
    {
        private readonly ServiceBusEntityType _entityType;
        private readonly string _entityName;
        private readonly ServiceBusNamespaceResource _ns;
        private readonly ILogger _logger;

        private TemporaryServiceBusEntity(ServiceBusEntityType entityType, string entityName, ServiceBusNamespaceResource @namespace, ILogger logger)
        {
            _entityType = entityType;
            _entityName = entityName;
            _ns = @namespace;
            _logger = logger;
        }

        public string EntityName { get; }

        public static async Task<TemporaryServiceBusEntity> CreateAsync(ServiceBusEntityType entityType, string entityName, TestConfig config, ILogger logger)
        {
            var armClient = new ArmClient(config.GetServicePrincipal().GetCredential());
            ServiceBusConfig serviceBus = config.GetServiceBus();
            ServiceBusNamespaceResource namespaceResource = armClient.GetServiceBusNamespaceResource(serviceBus.ResourceId);

            

            switch (entityType)
            {
                case ServiceBusEntityType.Queue:
                    logger.LogTrace("Create Service bus queue '{EntityName}'", entityName);
                    await namespaceResource.GetServiceBusQueues()
                                           .CreateOrUpdateAsync(WaitUntil.Completed, entityName, new ServiceBusQueueData());
                    break;

                case ServiceBusEntityType.Topic:
                    logger.LogTrace("Create Service bus topic '{EntityName}'", entityName);
                    ArmOperation<ServiceBusTopicResource> operation = await namespaceResource.GetServiceBusTopics()
                                                                                                 .CreateOrUpdateAsync(WaitUntil.Completed, entityName, new ServiceBusTopicData());
                    break;

                case ServiceBusEntityType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service bus entity type");
            }

            return new TemporaryServiceBusEntity(entityType, entityName, namespaceResource, logger);
        }

        public async ValueTask DisposeAsync()
        {
            switch (_entityType)
            {
                case ServiceBusEntityType.Queue:
                    _logger.LogTrace("Delete Service bus queue '{EntityName}'", _entityName);
                    ServiceBusQueueResource queue = await _ns.GetServiceBusQueueAsync(_entityName);
                    await queue.DeleteAsync(WaitUntil.Started);
                    break;
                
                case ServiceBusEntityType.Topic:
                    _logger.LogTrace("Delete Service bus topic '{EntityName}'", _entityName);
                    ServiceBusTopicResource topic = await _ns.GetServiceBusTopicAsync(_entityName);
                    await topic.DeleteAsync(WaitUntil.Started);
                    break;
                
                case ServiceBusEntityType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
