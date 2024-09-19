using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.ServiceBus;

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
            string namespaceConnectionString)
        {
            ServicePrincipal = servicePrincipal;
            NamespaceConnectionString = namespaceConnectionString;
            HostName = $"{@namespace}.servicebus.windows.net";
            ResourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{@namespace}");
        }

        public string HostName { get; }
        public ResourceIdentifier ResourceId { get; }
        public ServicePrincipal ServicePrincipal { get; }
        public string NamespaceConnectionString { get; }

        public ServiceBusClient GetClient()
        {
            return new ServiceBusClient(HostName, ServicePrincipal.GetCredential());
        }
    }

    public static class ServiceBusConfigExtensions
    {
        public static ServiceBusConfig GetServiceBus(this TestConfig config)
        {
            return new ServiceBusConfig(
                config.GetServicePrincipal(),
                config["Arcus:Infra:SubscriptionId"],
                config["Arcus:Infra:ResourceGroup:Name"],
                config["Arcus:ServiceBus:Namespace"],
                config["Arcus:ServiceBus:ConnectionString"]);
        }
    }
}
