using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ServiceBus;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents the contract on how to authenticate with the Azure Service Bus.
    /// </summary>
    [Obsolete("Will be removed in v3.0, please use Microsoft's built-in Azure SDK clients to construct " + nameof(ServiceBusManagementClient) + " instances")]
    public interface IAzureServiceBusManagementAuthentication
    {
        /// <summary>
        /// Authenticates with to the previously specified Azure Service Bus resource.
        /// </summary>
        /// <returns>
        ///     An <see cref="IServiceBusManagementClient"/> instance that manages the previously specified Azure Service Bus resource.
        /// </returns>
        Task<IServiceBusManagementClient> AuthenticateAsync();
    }
}
