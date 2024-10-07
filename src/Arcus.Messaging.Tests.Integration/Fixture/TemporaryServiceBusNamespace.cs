using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    internal class TemporaryServiceBusNamespace : IAsyncDisposable
    {
        private const string DefaultRuleName = "RootManageSharedAccessKey";

        private readonly ServiceBusNamespaceResource _namespace;
        private readonly ILogger _logger;

        private TemporaryServiceBusNamespace(
            ServicePrincipal servicePrincipal,
            ServiceBusNamespaceResource serviceBusNamespace,
            ILogger logger)
        {
            _namespace = serviceBusNamespace;
            _logger = logger;

            Config = new ServiceBusConfig(servicePrincipal, _namespace.Id.SubscriptionId, _namespace.Id.ResourceGroupName, _namespace.Id.Name);
        }

        public ServiceBusConfig Config { get; }

        public static async Task<TemporaryServiceBusNamespace> CreateBasicAsync(
            string subscriptionId,
            string resourceGroupName,
            ServicePrincipal servicePrincipal,
            ILogger logger)
        {
            var client = new ArmClient(servicePrincipal.GetCredential());

            string @namespace = $"arcus-message-servicebus-{Guid.NewGuid().ToString()[..10]}";
            logger.LogTrace("[Test] create Service bus namespace '{Namespace}'", @namespace);

            ResourceGroupResource resourceGroup = 
                await client.GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName)).GetAsync();

            var options = new ServiceBusNamespaceData(resourceGroup.Data.Location)
            {
                Sku = new ServiceBusSku(ServiceBusSkuName.Basic),
                DisableLocalAuth = false
            };
            ArmOperation<ServiceBusNamespaceResource> serviceBusNamespace = 
                await resourceGroup.GetServiceBusNamespaces()
                                   .CreateOrUpdateAsync(WaitUntil.Completed, @namespace, options);

            ResourceIdentifier serviceBusOwner = 
                RoleAssignmentResource.CreateResourceIdentifier(
                    scope: serviceBusNamespace.Value.Id.ToString(), 
                    "090c5cfd-751d-490a-894a-3ce6f1109419");

            var content = new RoleAssignmentCreateOrUpdateContent(serviceBusOwner, servicePrincipal.ObjectId);
            await client.GetRoleAssignments(serviceBusNamespace.Value.Id)
                        .CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentName: Guid.NewGuid().ToString(), content);

            return new TemporaryServiceBusNamespace(servicePrincipal, serviceBusNamespace.Value, logger);
        }

        public async Task<ServiceBusAccessKeys> GetAccessKeysAsync()
        {
            ServiceBusNamespaceAuthorizationRuleResource rule = GetDefaultAuthRule();

            ServiceBusAccessKeys keys = await rule.GetKeysAsync();
            return keys;
        }

        public async Task<ServiceBusAccessKeys> RotateAccessKeysAsync(ServiceBusAccessKeyType keyType)
        {
            _logger.LogTrace("[Test] rotate {KeyType} Service bus namespace '{Namespace}' connection string", keyType, _namespace.Id.Name);
            ServiceBusNamespaceAuthorizationRuleResource rule = GetDefaultAuthRule();

            ServiceBusAccessKeys newKeys =
                await rule.RegenerateKeysAsync(new ServiceBusRegenerateAccessKeyContent(keyType));

            return newKeys;
        }

        private ServiceBusNamespaceAuthorizationRuleResource GetDefaultAuthRule()
        {
            return _namespace.GetServiceBusNamespaceAuthorizationRule(DefaultRuleName);
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("[Test] delete Service bus namespace '{Namespace}'", _namespace.Id.Name);
            await _namespace.DeleteAsync(WaitUntil.Started);
        }
    }
}
