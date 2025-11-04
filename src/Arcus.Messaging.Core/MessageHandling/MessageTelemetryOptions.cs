using System;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the consumer configurable options model to change the behavior of the tracked telemetry.
    /// </summary>
    public class MessageTelemetryOptions
    {
        private string _operationName = "Service Bus message processing";

        /// <summary>
        /// Gets or sets the name of the operation that is used when a request telemetry is tracked - default 'Process' is used as operation name.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string OperationName
        {
            get => _operationName;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _operationName = value;
            }
        }
    }
}
