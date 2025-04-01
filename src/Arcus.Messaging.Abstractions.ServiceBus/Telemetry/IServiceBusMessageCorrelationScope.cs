using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Abstractions.ServiceBus.Telemetry
{
    /// <summary>
    /// Represents the way how incoming Azure Service bus request messages
    /// within a service-to-service correlation operation are tracked in a custom telemetry system,.
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
