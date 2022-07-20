using System;
using System.Threading;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.ServiceBus.Core
{
    /// <summary>
    /// Represents the user-configurable options to influence the message correlation tracking behavior of the <see cref="ServiceBusSenderExtensions.SendMessageAsync(ServiceBusSender,ServiceBusMessage,MessageCorrelationInfo,ILogger,Action{ServiceBusSenderMessageCorrelationOptions},CancellationToken)"/> extensions.
    /// </summary>
    public class ServiceBusSenderMessageCorrelationOptions
    {
        private string _transactionIdPropertyName = PropertyNames.TransactionId;
        private string _upstreamServicePropertyName = PropertyNames.OperationParentId;
        private Func<string> _generateDependencyId = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Azure Service Bus message application property name
        /// where the correlation transaction ID should be added when tracking Azure Service Bus dependencies.
        /// </summary>
        public string TransactionIdPropertyName
        {
            get => _transactionIdPropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank value for the message correlation transaction ID Azure Service Bus application property name");
                _transactionIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the Azure Service Bus message application property name
        /// where the dependency ID (generated via <see cref="GenerateDependencyId"/>) should be added when tracking Azure Service Bus dependencies.
        /// </summary>
        public string UpstreamServicePropertyName
        {
            get => _upstreamServicePropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank value for the message correlation upstream service Azure Service Bus application property name");
                _upstreamServicePropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the Azure Service Bus entity when tracking Azure Service Bus dependencies.
        /// </summary>
        public ServiceBusEntityType EntityType { get; set; }

        /// <summary>
        /// Gets or sets the function to generate the dependency ID used when tracking Azure Service Bus dependencies.
        /// </summary>
        public Func<string> GenerateDependencyId
        {
            get => _generateDependencyId;
            set
            {
                Guard.NotNull(value, nameof(value), "Requires a function to generate the dependency ID used when tracking Azure Service Bus dependencies");
                _generateDependencyId = value;
            }
        }
    }
}