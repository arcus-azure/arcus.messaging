using System;
using Arcus.Observability.Correlation;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents the information concerning correlation of telemetry &amp; processes with main focus on messaging scenarios.
    /// </summary>
    public class MessageCorrelationInfo : CorrelationInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfo"/> class.
        /// </summary>
        /// <param name="operationId">The unique identifier that spans one operation end-to-end.</param>
        /// <param name="transactionId">The unique identifier that spans one or more operations and are considered a transaction/session.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="operationId"/> is blank.</exception>
        public MessageCorrelationInfo(string operationId, string transactionId)
            : base(operationId, transactionId, operationParentId: null)
        {
            CycleId = Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfo" /> class.
        /// </summary>
        /// <param name="operationId">The unique identifier that spans one operation end-to-end.</param>
        /// <param name="transactionId">The unique identifier that spans one or more operations and are considered a transaction/session.</param>
        /// <param name="operationParentId">The unique identifier of the original service that initiated this request.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="operationId"/> is blank.</exception>
        public MessageCorrelationInfo(string operationId, string transactionId, string operationParentId)
            : base(operationId, transactionId, operationParentId)
        {
            CycleId = Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Gets the unique identifier that indicates an attempt to process a given message.
        /// </summary>
        /// <remarks>
        ///     If the same message is processed n-times, it will have the same OperationId but n different cycle ids
        /// </remarks>
        public string CycleId { get; }
    }
}