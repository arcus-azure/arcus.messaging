namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    ///     Information related to correlation of telemetry & processes
    /// </summary>
    /// <remarks>This is a copy of what is in Arcus WebAPI until it's been refactored into a dedicated package</remarks>
    public class CorrelationInfo
    {
        /// <summary>
        ///     Unique identifier that spans one operation end-to-end
        /// </summary>
        /// <example>Creating an order via an API that is persisted asynchronously by a message worker</example>
        public string OperationId { get; }

        /// <summary>
        ///     Unique identifier that spans one or more operations and are considered a transaction/session
        /// </summary>
        /// <example>User interacting with a platform</example>
        public string TransactionId { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="transactionId">
        ///     Unique identifier that spans one or more operations and are considered a
        ///     transaction/session
        /// </param>
        /// <param name="operationId">Unique identifier that spans one operation end-to-end</param>
        public CorrelationInfo(string transactionId, string operationId)
        {
            OperationId = operationId;
            TransactionId = transactionId;
        }
    }
}