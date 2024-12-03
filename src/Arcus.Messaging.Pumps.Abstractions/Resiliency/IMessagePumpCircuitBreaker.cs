using System;
using System.Threading.Tasks;

namespace Arcus.Messaging.Pumps.Abstractions.Resiliency
{
    /// <summary>
    /// Represents an instance to pause the process of receiving messages in the message pump until the message handler can process the messages again.
    /// Usually injected in the message handler to handle transient connection failures with dependencies.
    /// </summary>
    public interface IMessagePumpCircuitBreaker
    {
        /// <summary>
        /// Gets the current circuit breaker state of message processing in the given message pump.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        MessagePumpCircuitState GetCircuitBreakerState(string jobId);

        /// <summary>
        /// Pause the process of receiving messages in the message pump for a period of time before careful retrying again.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <param name="configureOptions">The optional user-configurable options to manipulate the workings of the message pump interaction.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        Task PauseMessageProcessingAsync(string jobId, Action<MessagePumpCircuitBreakerOptions> configureOptions);
    }

    /// <summary>
    /// Represents the available states in which the <see cref="MessagePump"/> is presently in within the circuit breaker context
    /// </summary>
    public sealed class MessagePumpCircuitState : IEquatable<MessagePumpCircuitState>
    {
        private enum CircuitBreakerState
        {
            Closed, HalfOpen, Open
        }

        private readonly CircuitBreakerState _state;

        private MessagePumpCircuitState(CircuitBreakerState state) : this(state, new MessagePumpCircuitBreakerOptions())
        {
        }

        private MessagePumpCircuitState(CircuitBreakerState state, MessagePumpCircuitBreakerOptions options)
        {
            _state = state;

            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets an instance of the <see cref="MessagePumpCircuitState"/> class that represents a closed state,
        /// in which the message pump is able to process messages normally.
        /// </summary>
        public static MessagePumpCircuitState Closed => new(CircuitBreakerState.Closed);

        /// <summary>
        /// Creates an instance of the <see cref="MessagePumpCircuitState"/> class that represents a half-open state,
        /// in which the message pump is under inspection if it can receive messages.
        /// </summary>
        public static MessagePumpCircuitState HalfOpen => new(CircuitBreakerState.HalfOpen);

        /// <summary>
        /// Gets the accompanied additional options that manipulate the behavior of any given state.
        /// </summary>
        public MessagePumpCircuitBreakerOptions Options { get; }

        /// <summary>
        /// Creates an instance of the <see cref="MessagePumpCircuitState"/> class that represents an open state,
        /// in which the message pump is unable to process messages.
        /// </summary>
        public static MessagePumpCircuitState Open() => Open(new MessagePumpCircuitBreakerOptions());

        /// <summary>
        /// Creates an instance of the <see cref="MessagePumpCircuitState"/> class that represents an open state,
        /// in which the message pump is unable to process messages.
        /// </summary>
        /// <param name="options">The additional options to configure the message pump during the half-open state.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public static MessagePumpCircuitState Open(MessagePumpCircuitBreakerOptions options)
        {
            return new MessagePumpCircuitState(CircuitBreakerState.Open, options);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
        public bool Equals(MessagePumpCircuitState other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return _state == other._state;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is MessagePumpCircuitState other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return (int) _state;
        }

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        public static bool operator ==(MessagePumpCircuitState left, MessagePumpCircuitState right)
        {
            return Equals(left, right);
        }

        
        /// <summary>
        /// Determines whether the specified objects are not equal.
        /// </summary>
        public static bool operator !=(MessagePumpCircuitState left, MessagePumpCircuitState right)
        {
            return !Equals(left, right);
        }
    }
}
