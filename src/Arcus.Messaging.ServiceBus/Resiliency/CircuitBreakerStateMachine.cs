using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.ServiceBus.Resiliency
{
    /// <summary>
    /// Coordinates thread-safe circuit breaker state transitions for a message pump job and publishes transition
    /// notifications.
    /// </summary>
    /// <remarks>Applies guarded transitions between Closed, Open, and HalfOpen states, logs each successful
    /// change, and invokes matching event handlers through the service provider outside the state lock to avoid
    /// blocking pump execution.</remarks>
    internal class CircuitBreakerStateMachine
    {
        private readonly object _lock = new();
        private readonly string _jobId;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private MessagePumpCircuitState _current = MessagePumpCircuitState.Closed;

        /// <summary>
        /// Initializes a new instance of the circuit breaker state machine for a specific MessagePump (job).
        /// </summary>
        /// <param name="jobId">Unique identifier of the job associated with the state machine.</param>
        /// <param name="serviceProvider">Service provider used to resolve required dependencies.</param>
        /// <param name="logger">Logger used to record state transitions and related events.</param>
        internal CircuitBreakerStateMachine(string jobId, IServiceProvider serviceProvider, ILogger logger)
        {
            _jobId = jobId;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>Gets a snapshot of the current state.</summary>
        internal MessagePumpCircuitState Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }

        /// <summary>
        /// Resets the circuit to <see cref="CircuitBreakerState.Closed"/> because the owning pump (re)started processing messages.
        /// </summary>
        internal void MarkPumpStarted() => TryTransition(s => !s.IsClosed, _ => MessagePumpCircuitState.Closed);

        /// <summary>
        /// Marks the circuit as <see cref="CircuitBreakerState.Open"/> because the owning pump stopped processing messages,
        /// so that subscribers and state queries reflect that no messages are being retrieved.
        /// </summary>
        internal void MarkPumpStopped() => TryTransition(s => !s.IsOpen, s => s.TransitionTo(CircuitBreakerState.Open));
        
        /// <summary>Closed → Open. Returns <c>false</c> if the circuit was already open or half-open (concurrent callers are no-ops).</summary>
        internal bool TryOpen(MessagePumpCircuitBreakerOptions options)
            => TryTransition(s => s.IsClosed, s => s.TransitionTo(CircuitBreakerState.Open, options));

        /// <summary>Open → HalfOpen.</summary>
        internal bool TryHalfOpen()
            => TryTransition(s => s.IsOpen, s => s.TransitionTo(CircuitBreakerState.HalfOpen));

        /// <summary>Open/HalfOpen → Closed.</summary>
        internal bool TryClose()
            => TryTransition(s => !s.IsClosed, _ => MessagePumpCircuitState.Closed);
        
        private bool TryTransition(Func<MessagePumpCircuitState, bool> precondition, Func<MessagePumpCircuitState, MessagePumpCircuitState> transition)
        {
            MessagePumpCircuitState oldState, newState;

            lock (_lock)
            {
                if (!precondition(_current))
                {
                    return false;
                }

                oldState = _current;
                newState = transition(oldState);
                _current = newState;
            }

            _logger.LogDebug("Circuit breaker caused message pump '{JobId}' to transition from '{OldState}' to '{NewState}'", _jobId, oldState, newState);
            Notify(oldState, newState);
            return true;
        }

        private void Notify(MessagePumpCircuitState oldState, MessagePumpCircuitState newState)
        {
            // Outside the lock on purpose: user code must not be able to block the pump.
            ICircuitBreakerEventHandler[] handlers =
                _serviceProvider.GetServices<CircuitBreakerEventHandler>()
                                .Where(r => r.JobId == _jobId)
                                .Select(r => r.Handler)
                                .ToArray();

            var args = new MessagePumpCircuitStateChangedEventArgs(_jobId, oldState, newState);
            foreach (ICircuitBreakerEventHandler handler in handlers)
            {
                _ = Task.Run(() => handler.OnTransition(args));
            }
        }
    }
}
