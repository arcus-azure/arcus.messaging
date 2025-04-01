using System;

namespace Arcus.Messaging.Abstractions.ServiceBus.Telemetry
{
    /// <summary>
    /// Represents the user-configurable options to manipulate telemetry-related configuration.
    /// </summary>
    public class AzureServiceBusMessageTelemetryOptions
    {
        private string _operationName;

        /// <summary>
        /// Gets or sets the name of the operation that is used when a request telemetry is tracked.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string OperationName
        {
            get => _operationName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank operation name", nameof(value));
                }

                _operationName = value;
            }
        }
    }
}
