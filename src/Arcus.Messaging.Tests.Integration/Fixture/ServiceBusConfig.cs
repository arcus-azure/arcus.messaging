using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class ServiceBusConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusConfig" /> class.
        /// </summary>
        public ServiceBusConfig(
            ServicePrincipal servicePrincipal,
            string @namespace)
        {
            ServicePrincipal = servicePrincipal;
            HostName = $"{@namespace}.servicebus.windows.net";
        }

        public string HostName { get; }
        public ServicePrincipal ServicePrincipal { get; }

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
                config["Arcus:ServiceBus:Namespace"]);
        }
    }
}
