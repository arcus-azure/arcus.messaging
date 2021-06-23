using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents the additional options to change the behavior of the Azure Service Bus message handling during the routing of the <see cref="AzureServiceBusMessageRouter"/>.
    /// </summary>
    public class AzureServiceBusMessageHandlerOptions
    {
        /// <summary>
        /// Gets or sets the instance that can call operations (dead letter, complete...) on an Azure Service Bus <see cref="ServiceBusReceivedMessage"/>;
        /// used within <see cref="AzureServiceBusMessageHandler{TMessage}"/>s or <see cref="AzureServiceBusFallbackMessageHandler"/>s with Azure Service Bus specific operations.
        /// </summary>
        public ServiceBusReceiver ServiceBusReceiver { get; set; }
    }
}
