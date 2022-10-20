using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents the correlation result of a received Azure Service Bus message.
    /// This result will act as the scope of the request telemetry.
    /// </summary>
    public class MessageCorrelationResult : IDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IOperationHolder<RequestTelemetry> _operationHolder;

        private MessageCorrelationResult(
            MessageCorrelationInfo correlationInfo,
            TelemetryClient client,
            IOperationHolder<RequestTelemetry> operationHolder)
        {
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a hierarchical message correlation information instance for the received message");
            Guard.NotNull(operationHolder, nameof(operationHolder), "Requires an operation holder to manage the scope where dependencies are automatically tracked");

            _telemetryClient = client;
            _operationHolder = operationHolder;
            CorrelationInfo = correlationInfo;
        }

        private MessageCorrelationResult(MessageCorrelationInfo correlationInfo)
        {
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a hierarchical message correlation information instance for the received message");
            CorrelationInfo = correlationInfo;
        }

        /// <summary>
        /// Gets the correlation information of the current received Azure Service Bus message.
        /// </summary>
        public MessageCorrelationInfo CorrelationInfo { get; }

        /// <summary>
        /// Creates an <see cref="MessageCorrelationResult"/> based on the <see cref="MessageCorrelationFormat.Hierarchical"/>.
        /// </summary>
        /// <param name="correlationInfo">The correlation information based on custom application properties.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="correlationInfo"/> is <c>null</c>.</exception>
        public static MessageCorrelationResult Create(MessageCorrelationInfo correlationInfo)
        {
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a hierarchical message correlation information instance for the received message");
            return new MessageCorrelationResult(correlationInfo);
        }

        /// <summary>
        /// Creates an <see cref="MessageCorrelationResult"/> based on the <see cref="MessageCorrelationFormat.W3C"/>.
        /// </summary>
        /// <param name="client">The telemetry client used for this message correlation.</param>
        /// <param name="transactionId">The cross-operation transaction ID of the message correlation.</param>
        /// <param name="operationParentId">The parent ID of the message correlation.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionId"/> or the <paramref name="operationParentId"/> is blank.</exception>
        public static MessageCorrelationResult Create(
            TelemetryClient client,
            string transactionId, 
            string operationParentId)
        {
            Guard.NotNullOrWhitespace(transactionId, nameof(transactionId), "Requires a transaction ID to determine the message correlation of the received Azure Service Bus message");
            Guard.NotNullOrWhitespace(operationParentId, nameof(operationParentId), "Requires a operation parent ID to determine the message correlation of the received Azure Service Bus message");

            var telemetry = new RequestTelemetry();
            telemetry.Context.Operation.Id = transactionId;
            telemetry.Context.Operation.ParentId = operationParentId;

            IOperationHolder<RequestTelemetry> operationHolder = client.StartOperation(telemetry);
            var correlationInfo = new MessageCorrelationInfo(telemetry.Id, transactionId, operationParentId);

            return new MessageCorrelationResult(correlationInfo, client, operationHolder);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.TelemetryConfiguration.DisableTelemetry = true;
            }

            _operationHolder?.Dispose();

            if (_telemetryClient != null)
            {
                _telemetryClient.TelemetryConfiguration.DisableTelemetry = false;
            }
        }
    }
}
