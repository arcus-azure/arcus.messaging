using System;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;

namespace Arcus.Messaging.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Represents the W3C message correlation for incoming Azure Service Bus messages in in-process Azure Functions environments.
    /// </summary>
    public class AzureFunctionsInProcessMessageCorrelation
    {
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsInProcessMessageCorrelation" /> class.
        /// </summary>
        /// <param name="client">The Microsoft telemetry client to automatically track outgoing built-in Microsoft dependencies.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/> is <c>null</c>.</exception>
        public AzureFunctionsInProcessMessageCorrelation(TelemetryClient client)
        {
            _telemetryClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Correlate the incoming Azure Service Bus message using the W3C message correlation.
        /// </summary>
        /// <param name="message">The incoming Azure Service Bus message.</param>
        /// <returns>The disposable message correlation telemetry request scope.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public MessageCorrelationResult CorrelateMessage(ServiceBusReceivedMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();
            return MessageCorrelationResult.Create(_telemetryClient, transactionId, operationParentId);
        }
    }
}
