using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.Telemetry;

namespace Arcus.Messaging.Abstractions.ServiceBus.Telemetry
{
    /// <summary>
    /// Represents an approach to track the correlation information of a received Azure Service Bus message within a message pump.
    /// </summary>
    public interface IServiceBusMessageCorrelationScope
    {
        /// <summary>
        /// Starts a new Azure Service bus request operation on the telemetry system.
        /// </summary>
        /// <param name="messageContext">The message context for the currently received Azure Service bus message.</param>
        /// <param name="options">The user-configurable options to manipulate the telemetry.</param>
        MessageOperationResult StartOperation(AzureServiceBusMessageContext messageContext, MessageTelemetryOptions options);
    }
}
