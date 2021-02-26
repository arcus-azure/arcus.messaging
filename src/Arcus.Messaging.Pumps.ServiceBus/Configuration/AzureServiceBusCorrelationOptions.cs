using Arcus.Messaging.Abstractions;
using GuardNet;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// Represents the user-configurable options to control the correlation information tracking during the receiving of the Azure Service Bus messages in the <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    public class AzureServiceBusCorrelationOptions
    {
        private string _transactionIdProperty = PropertyNames.TransactionId;
        
        /// <summary>
        /// Gets or sets the name of the Azure Service Bus message property to determine the transaction ID.
        /// </summary>
        public string TransactionIdPropertyName
        {
            get => _transactionIdProperty;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Transaction ID message property name for Azure Service Bus Message cannot be blank");
                _transactionIdProperty = value;
            }
        }
    }
}
