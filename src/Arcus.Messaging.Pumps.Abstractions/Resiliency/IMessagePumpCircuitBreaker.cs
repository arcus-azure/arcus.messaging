﻿using System;
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
    /// Represents the available states in the <see cref="MessagePumpCircuitState"/> in which the message pump can transition into.
    /// </summary>
    internal enum CircuitBreakerState
    {
        Closed, HalfOpen, Open
    }

    /// <summary>
    /// Represents the available states in which the <see cref="MessagePump"/> is presently in within the circuit breaker context
    /// </summary>
    public sealed class MessagePumpCircuitState
    {
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
        /// Gets the boolean flag that indicates whether the message pump circuit breaker state is in the 'Closed' state,
        /// and should start retrieving messages.
        /// </summary>
        public bool IsClosed => _state is CircuitBreakerState.Closed;

        /// <summary>
        /// Gets the boolean flag that indicates whether the message pump circuit breaker state is in the 'Open' state,
        /// and should stop retrieving messages.
        /// </summary>
        public bool IsOpen => _state is CircuitBreakerState.Open;

        /// <summary>
        /// Gets the accompanied additional options that manipulate the behavior of any given state.
        /// </summary>
        public MessagePumpCircuitBreakerOptions Options { get; }

        /// <summary>
        /// Gets an instance of the <see cref="MessagePumpCircuitState"/> class that represents a closed state,
        /// in which the message pump is able to process messages normally.
        /// </summary>
        internal static MessagePumpCircuitState Closed => new(CircuitBreakerState.Closed);

        /// <summary>
        /// Creates an instance of the <see cref="MessagePumpCircuitState"/> class that represents an open state,
        /// in which the message pump is unable to process messages.
        /// </summary>
        /// <param name="options">The additional options to configure the message pump during the open state.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        internal static MessagePumpCircuitState Open(MessagePumpCircuitBreakerOptions options)
        {
            return new(CircuitBreakerState.Open, options);
        }

        /// <summary>
        /// Lets the current instance of the state transition to another state.
        /// </summary>
        internal MessagePumpCircuitState TransitionTo(CircuitBreakerState state)
        {
            return new(state, Options);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _state.ToString();
        }
    }
}
