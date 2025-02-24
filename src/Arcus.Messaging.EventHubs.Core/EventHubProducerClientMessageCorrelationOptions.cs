using System;
using System.Collections.Generic;
using System.Threading;
using Arcus.Messaging.Abstractions;
using Arcus.Observability.Correlation;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618 // All EventHubs-functionality will be removed anyway, so ignore deprecated correlation properties.

namespace Arcus.Messaging.EventHubs.Core
{
    /// <summary>
    /// Represents the user-configurable options to influence the message correlation tracking behavior of the <see cref="EventHubProducerClientExtensions.SendAsync(EventHubProducerClient,IEnumerable{EventData},CorrelationInfo,ILogger,SendEventOptions,Action{EventHubProducerClientMessageCorrelationOptions},CancellationToken)"/> extensions.
    /// </summary>
    public class EventHubProducerClientMessageCorrelationOptions
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
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank value for the message correlation transaction ID Azure Service Bus application property name", nameof(value));
                }

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
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank value for the message correlation upstream service Azure Service Bus application property name", nameof(value));
                }

                _upstreamServicePropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the function to generate the dependency ID used when tracking Azure Service Bus dependencies.
        /// </summary>
        public Func<string> GenerateDependencyId
        {
            get => _generateDependencyId;
            set => _generateDependencyId = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the telemetry context used during the Azure EventHubs dependency tracking.
        /// </summary>
        internal Dictionary<string, object> TelemetryContext { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds a telemetry context while tracking the Azure EventHubs dependency.
        /// </summary>
        /// <param name="telemetryContext">The dictionary with contextual information about the dependency telemetry.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="telemetryContext"/> is <c>null</c>.</exception>
        public void AddTelemetryContext(Dictionary<string, object> telemetryContext)
        {
            if (telemetryContext is null)
            {
                throw new ArgumentNullException(nameof(telemetryContext));
            }

            foreach (KeyValuePair<string, object> item in telemetryContext)
            {
                TelemetryContext[item.Key] = item.Value;
            }
        }
    }
}