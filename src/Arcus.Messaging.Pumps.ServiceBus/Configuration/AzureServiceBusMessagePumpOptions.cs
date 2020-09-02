using System;
using GuardNet;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration 
{
    /// <summary>
    /// The general options that configures a <see cref="AzureServiceBusMessagePump"/> implementation.
    /// </summary>
    public class AzureServiceBusMessagePumpOptions
    {
        private int? _maxConcurrentCalls;
        private string _jobId = Guid.NewGuid().ToString();
        private TimeSpan _keyRotationTimeout = TimeSpan.FromSeconds(5);
        private int _maximumUnauthorizedExceptionsBeforeRestart = 5;

        /// <summary>
        ///     Maximum concurrent calls to process messages
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
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
        public bool AutoComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than <see cref="TimeSpan.Zero"/>.</exception>
        public TimeSpan KeyRotationTimeout
        {
            get => _keyRotationTimeout;
            set
            {
                Guard.NotLessThan(value, TimeSpan.Zero, nameof(value), "Key rotation timeout cannot be less than a zero time range");
                _keyRotationTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the fallback when the Azure Key Vault notification doesn't get delivered correctly,
        /// how many times should the message pump run into an <see cref="UnauthorizedException"/> before restarting.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        public int MaximumUnauthorizedExceptionsBeforeRestart
        {
            get => _maximumUnauthorizedExceptionsBeforeRestart;
            set
            {
                Guard.NotLessThan(value, 0, nameof(value), "Requires an unauthorized exceptions count that's greater than zero");
                _maximumUnauthorizedExceptionsBeforeRestart = value;
            }
        }
    }
}