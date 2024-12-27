using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    /// <summary>
    /// Represents an <see cref="ICircuitBreakerEventHandler"/> implementation that verifies if the circuit breaker transitions states are correct.
    /// </summary>
    internal class MockCircuitBreakerEventHandler : ICircuitBreakerEventHandler
    {
        private readonly Collection<MessagePumpCircuitState> _states = new();

        /// <summary>
        /// Notifies the application on a change in the message pump's circuit breaker state.
        /// </summary>
        /// <param name="newState">The new circuit breaker state in which the message pump is currently running on.</param>
        public Task OnTransitionAsync(MessagePumpCircuitState newState)
        {
            _states.Add(newState);

            return Task.CompletedTask;
        }

        public void ShouldTransitionCorrectly()
        {
            Assert.NotEmpty(_states);

            MessagePumpCircuitState firstTransition = _states[0];
            Assert.True(firstTransition.IsOpen, $"if the message pump starts up, the first transition should always be from a closed to open state, but got: {firstTransition}");

            IEnumerable<(MessagePumpCircuitState oldState, MessagePumpCircuitState newState)> transitions =
                _states.SkipLast(1).Zip(_states.Skip(1));

            Assert.All(transitions, t => VerifyCorrectTransition(t.oldState, t.newState));
        }

        private static void VerifyCorrectTransition(
            MessagePumpCircuitState oldState,
            MessagePumpCircuitState newState)
        {
            if (oldState.IsClosed)
            {
                Assert.True(newState.IsHalfOpen, $"if the message pump comes from a closed state, the next state should always be half-open, but got: {newState}");
            }
            else if (oldState.IsOpen)
            {
                Assert.True(newState.IsHalfOpen, $"if the message pump comes from a open state, the next state should always be half-open, but got: {newState}");
            }
            else if (oldState.IsHalfOpen)
            {
                Assert.True(newState.IsClosed, $"if the message pump comes from a half-open state, the next state should always be closed, but got: {newState}");
            }
        }
    }
}
