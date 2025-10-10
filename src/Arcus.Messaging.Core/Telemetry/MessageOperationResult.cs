using System;
using System.Diagnostics;

namespace Arcus.Messaging.Abstractions.Telemetry
{
    /// <summary>
    /// Represents the result of a request tracking operation.
    /// </summary>
    public abstract class MessageOperationResult : IDisposable
    {
        private readonly DateTimeOffset _startTime;
        private readonly Stopwatch _watch;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageOperationResult"/> class.
        /// </summary>
        /// <param name="correlationInfo">The correlation information of the current received message.</param>
        protected MessageOperationResult(MessageCorrelationInfo correlationInfo)
        {
            _startTime = DateTimeOffset.UtcNow;
            _watch = Stopwatch.StartNew();

            Correlation = correlationInfo;
        }

        /// <summary>
        /// Gets the correlation information of the current received message.
        /// </summary>
        public MessageCorrelationInfo Correlation { get; }

        /// <summary>
        /// Gets or sets the boolean flag to indicate that the tracked operation for the correlated context was successful.
        /// </summary>
        /// <remarks>
        ///     Used in telemetry tracking systems as a way to provide additional context on the operation.
        /// </remarks>
        public bool IsSuccessful { get; set; }

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
            StopOperation(IsSuccessful, _startTime, _watch.Elapsed);
        }

        /// <summary>
        /// Finalizes the tracked operation in the concrete telemetry system, based on the operation results.
        /// </summary>
        /// <param name="isSuccessful">The boolean flag to indicate whether the operation was successful.</param>
        /// <param name="startTime">The date when the operation started.</param>
        /// <param name="duration">The time it took for the operation to run.</param>
        protected abstract void StopOperation(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration);
    }
}
