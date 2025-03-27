using System;
using System.Diagnostics;
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
    public class MessageCorrelationResult : IDisposable
    {
        private readonly DateTimeOffset _startTime;
        private readonly Stopwatch _watch;
        private readonly Action<(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)> _onOperationFinished;

        private MessageCorrelationResult(
            MessageCorrelationInfo correlationInfo,
            Action<(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)> onOperationFinished)
        {
            _onOperationFinished = onOperationFinished;
            _startTime = DateTimeOffset.UtcNow;
            _watch = Stopwatch.StartNew();

            CorrelationInfo = correlationInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationResult"/> class.
        /// </summary>
        protected MessageCorrelationResult(MessageCorrelationInfo correlation)
        {
            CorrelationInfo = correlation ?? throw new ArgumentNullException(nameof(correlation));
        }

        /// <summary>
        /// Gets or sets the boolean flag to indicate that the tracked operation for the correlated context was successful.
        /// </summary>
        /// <remarks>
        ///     Used in telemetry tracking systems as a way to provide additional context on the operation.
        /// </remarks>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets the correlation information of the current received Azure Service Bus message.
        /// </summary>
        public MessageCorrelationInfo CorrelationInfo { get; }

        /// <summary>
        /// Creates an <see cref="MessageCorrelationResult"/> based on the <see cref="MessageCorrelationFormat.Hierarchical"/>.
        /// </summary>
        /// <param name="correlationInfo">The correlation information based on custom application properties.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="correlationInfo"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as the Hierarchical correlation format is deprecated, please use W3C instead")]
        public static MessageCorrelationResult Create(MessageCorrelationInfo correlationInfo)
        {
            return new MessageCorrelationResult(correlationInfo ?? throw new ArgumentNullException(nameof(correlationInfo)), _ => { });
        }

        /// <summary>
        /// Creates an <see cref="MessageCorrelationResult"/> based on the <see cref="MessageCorrelationFormat.W3C"/>.
        /// </summary>
        /// <param name="client">The telemetry client used for this message correlation.</param>
        /// <param name="transactionId">The cross-operation transaction ID of the message correlation.</param>
        /// <param name="operationParentId">The parent ID of the message correlation.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionId"/> or the <paramref name="operationParentId"/> is blank.</exception>
        [Obsolete("Will be moved in v3.0, inherit from " + nameof(MessageCorrelationResult) + " instead to custom track correlation in Application Insights")]
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

            return new MessageCorrelationResult(correlationInfo, onFinished =>
            {
                client.TelemetryConfiguration.DisableTelemetry = true;
                operationHolder.Dispose();
                client.TelemetryConfiguration.DisableTelemetry = false;
            });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            _watch.Stop();
            OnOperationFinished(IsSuccessful, _startTime, _watch.Elapsed);
        }

        /// <summary>
        /// Finalizes the tracked operation in the concrete telemetry system, based on the operation results.
        /// </summary>
        /// <param name="isSuccessful">The boolean flag to indicate whether the operation was successful.</param>
        /// <param name="startTime">The date when the operation started.</param>
        /// <param name="duration">The time it took for the operation to run.</param>
        protected virtual void OnOperationFinished(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)
        {
            _onOperationFinished?.Invoke((isSuccessful, startTime, duration));
        }
    }
}
