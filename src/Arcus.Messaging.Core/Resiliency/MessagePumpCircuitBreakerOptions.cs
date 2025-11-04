using System;

namespace Arcus.Messaging.Pumps.Abstractions.Resiliency
{
    /// <summary>
    /// Represents user-configurable options to manipulate the <see cref="IMessagePumpCircuitBreaker"/> functionality.
    /// </summary>
    public class MessagePumpCircuitBreakerOptions
    {
        private TimeSpan _messageRecoveryPeriod = TimeSpan.FromSeconds(30),
                         _messageIntervalDuringRecovery = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the time period the circuit breaker should wait before retrying to receive messages.
        /// A.k.a. the time period the circuit is open.
        /// </summary>
        /// <remarks>
        ///     Default uses 30 seconds recovery period.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> does not represent a positive time period.</exception>
        public TimeSpan MessageRecoveryPeriod
        {
            get => _messageRecoveryPeriod;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _messageRecoveryPeriod = value;
            }
        }

        /// <summary>
        /// Gets or sets the time period the circuit breaker should wait between each message after the circuit was closed, during recovery.
        /// A.k.a. the time interval to receive messages during which the circuit is half-open.
        /// </summary>
        /// <remarks>
        ///     Default uses 10 seconds interval period.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> does not represent a positive time period.</exception>
        public TimeSpan MessageIntervalDuringRecovery
        {
            get => _messageIntervalDuringRecovery;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _messageIntervalDuringRecovery = value;
            }
        }
    }
}
