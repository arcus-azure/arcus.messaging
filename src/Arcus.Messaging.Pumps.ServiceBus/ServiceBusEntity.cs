using System;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents the Azure Service Bus entity type.
    /// </summary>
    [Obsolete("Use the built-in " + nameof(ServiceBusEntityType) + " enumeration instead")]
    public enum ServiceBusEntity
    {
        /// <summary>
        /// Uses the Service Bus Queue entity.
        /// </summary>
        Queue = 1,

        /// <summary>
        /// Uses the Service Bus Topic entity.
        /// </summary>
        Topic = 2
    }
}