using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents the correlation result of a received Azure Service Bus message.
    /// This result will act as the scope of the request telemetry.
    /// </summary>
    public sealed class MessageCorrelationResult : IDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IOperationHolder<RequestTelemetry> _operationHolder;

        private MessageCorrelationResult(
            MessageCorrelationInfo correlationInfo,
            TelemetryClient client,
            IOperationHolder<RequestTelemetry> operationHolder)
        {
            _telemetryClient = client;
            _operationHolder = operationHolder ?? throw new ArgumentNullException(nameof(operationHolder));
            CorrelationInfo = correlationInfo ?? throw new ArgumentNullException(nameof(correlationInfo));
        }

        private MessageCorrelationResult(MessageCorrelationInfo correlationInfo)
        {
            CorrelationInfo = correlationInfo ?? throw new ArgumentNullException(nameof(correlationInfo));
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
            return new MessageCorrelationResult(correlationInfo ?? throw new ArgumentNullException(nameof(correlationInfo)));
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
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException("Requires a non-blank transaction ID to determine the message correlation", nameof(transactionId));
            }

            if (string.IsNullOrWhiteSpace(operationParentId))
            {
                throw new ArgumentException("Requires a non-blank operation parent ID to determine the message correlation");
            }

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
