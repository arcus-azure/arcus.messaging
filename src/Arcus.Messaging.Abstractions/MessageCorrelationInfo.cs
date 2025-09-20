using System;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents the information concerning correlation of telemetry &amp; processes with main focus on messaging scenarios.
    /// </summary>
    public class MessageCorrelationInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfo"/> class.
        /// </summary>
        /// <param name="operationId">The unique identifier that spans one operation end-to-end.</param>
        /// <param name="transactionId">The unique identifier that spans one or more operations and are considered a transaction/session.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="operationId"/> or <paramref name="transactionId"/> is blank.</exception>
        public MessageCorrelationInfo(string operationId, string transactionId)
            : this(operationId, transactionId, operationParentId: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfo" /> class.
        /// </summary>
        /// <param name="operationId">The unique identifier that spans one operation end-to-end.</param>
        /// <param name="transactionId">The unique identifier that spans one or more operations and are considered a transaction/session.</param>
        /// <param name="operationParentId">The unique identifier of the original service that initiated this request.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="operationId"/>, <paramref name="transactionId"/> or <paramref name="operationParentId"/> is blank.
        /// </exception>
        public MessageCorrelationInfo(string operationId, string transactionId, string operationParentId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
            ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

            OperationId = operationId;
            TransactionId = transactionId;
            OperationParentId = operationParentId;

            CycleId = Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Gets the ID that relates different messaging requests together in a single transaction.
        /// </summary>
        public string TransactionId { get; }

        /// <summary>
        /// Gets the unique ID information of the messaging request.
        /// </summary>
        public string OperationId { get; }

        /// <summary>
        /// Gets the ID of the original service that initiated this messaging request.
        /// </summary>
        public string OperationParentId { get; }

        /// <summary>
        /// Gets the unique identifier that indicates an attempt to process a given message.
        /// </summary>
        /// <remarks>
        ///     If the same message is processed n-times, it will have the same OperationId but n different cycle ids
        /// </remarks>
        public string CycleId { get; }
    }
}