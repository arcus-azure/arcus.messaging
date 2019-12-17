using System;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    ///     Information concerning correlation of telemetry & processes with main focus on messaging scenarios
    /// </summary>
    public class MessageCorrelationInfo : CorrelationInfo
    {
        /// <summary>
        ///     Unique identifier that indicates an attempt to process a given message
        /// </summary>
        /// <remarks>
        ///     If the same message is processed n-times,
        ///     it will have the same OperationId but n different cycle ids
        /// </remarks>
        public string CycleId { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="transactionId">
        ///     Unique identifier that spans one or more operations and are considered a
        ///     transaction/session
        /// </param>
        /// <param name="operationId">Unique identifier that spans one operation end-to-end</param>
        public MessageCorrelationInfo(string transactionId, string operationId)
            : base(transactionId, operationId)
        {
            CycleId = Guid.NewGuid().ToString("D");
        }
    }
}