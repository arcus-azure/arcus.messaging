using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager.ServiceBus;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class ServiceBusConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusConfig" /> class.
        /// </summary>
        public ServiceBusConfig(
            ServicePrincipal servicePrincipal,
            string subscriptionId,
            string resourceGroupName,
            string @namespace,
            string namespaceConnectionString = null)
        {
            ServicePrincipal = servicePrincipal;
            NamespaceConnectionString = namespaceConnectionString;
            HostName = $"{@namespace}.servicebus.windows.net";
            ResourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, @namespace);
        }

        public string HostName { get; }
        public ResourceIdentifier ResourceId { get; }
        public ServicePrincipal ServicePrincipal { get; }
        public string NamespaceConnectionString { get; }

        public ServiceBusClient GetClient()
        {
            return new ServiceBusClient(HostName, ServicePrincipal.GetCredential());
        }

        public ServiceBusAdministrationClient GetAdminClient()
        {
            return new ServiceBusAdministrationClient(HostName, ServicePrincipal.GetCredential());
        }
    }

    public static class ServiceBusConfigExtensions
    {
        public static ServiceBusConfig GetServiceBus(this TestConfig config)
        {
            return new ServiceBusConfig(
                config.GetServicePrincipal(),
                config.GetSubscriptionId(),
                config.GetResourceGroupName(),
                config["Arcus:ServiceBus:Namespace"],
                config["Arcus:ServiceBus:ConnectionString"]);
        }
    }
}
