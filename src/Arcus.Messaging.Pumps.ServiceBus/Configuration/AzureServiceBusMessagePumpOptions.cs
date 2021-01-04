using System;
using GuardNet;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration 
{
    /// <summary>
    /// The general options that configures a <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    public class AzureServiceBusMessagePumpOptions
    {
        private int? _maxConcurrentCalls;
        private string _jobId;
        private TimeSpan _keyRotationTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        ///     Maximum concurrent calls to process messages
        /// </summary>
        public int? MaxConcurrentCalls
        {
            get => _maxConcurrentCalls;
            set
            {
                if (value != null)
                {
                    Guard.For<ArgumentException>(() => value <= 0, "Max concurrent calls has to be 1 or above.");
                }

                _maxConcurrentCalls = value;
            }
        }

        /// <summary>
        ///     Indication whether or not messages should be automatically marked as completed if no exceptions occured and
        ///     processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed</remarks>
        public bool AutoComplete { get; set; }

        /// <summary>
        /// Gets or sets the flag to indicate whether or not to emit security events during the lifetime of the message pump.
        /// </summary>
        public bool EmitSecurityEvents { get; set; } = false;

        /// <summary>
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId
        {
            get => _jobId;
            set
            {
                Guard.NotNullOrEmpty(value, nameof(value), "Unique identifier for background job cannot be empty");
                _jobId = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout when the message pump tries to restart and re-authenticate during key rotation.
        /// </summary>
        public TimeSpan KeyRotationTimeout
        {
            get => _keyRotationTimeout;
            set
            {
                Guard.NotLessThan(value, TimeSpan.Zero, nameof(value), "Key rotation timeout cannot be less than a zero time range");
                _keyRotationTimeout = value;
            }
        }
    }
}