using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Testing;
using Xunit;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    /// <summary>
    /// Represents an <see cref="ICircuitBreakerEventHandler"/> implementation that verifies if the circuit breaker transitions states are correct.
    /// </summary>
    internal class MockCircuitBreakerEventHandler : ICircuitBreakerEventHandler
    {
        private readonly Collection<MessagePumpCircuitStateChangeEventArgs> _states = new();

        /// <summary>
        /// Notifies the application on a change in the message pump's circuit breaker state.
        /// </summary>
        /// <param name="args">The change in the circuit breaker state for a message pump.</param>
        public void OnTransition(MessagePumpCircuitStateChangeEventArgs args)
        {
            _states.Add(args);
        }

        /// <summary>
        /// Verifies that all fired circuit breaker state transitions are happening correctly.
        /// </summary>
        public async Task ShouldTransitionedCorrectlyAsync()
        {
            await Poll.Target<XunitException>(() =>
            {
                Assert.NotEmpty(_states);
                Assert.Equal(3, _states.Count);

            }).Every(TimeSpan.FromSeconds(1))
              .Timeout(TimeSpan.FromSeconds(10))
              .FailWith("could not in time find all the fired circuit breaker change events, possibly the message pump did not fired them");

            Assert.All(_states, t => VerifyCorrectTransition(t.OldState, t.NewState));
        }

        private static void VerifyCorrectTransition(
            MessagePumpCircuitState oldState,
            MessagePumpCircuitState newState)
        {
            if (oldState.IsClosed)
            {
                Assert.True(newState.IsOpen, $"when the message pump comes from a closed state, the next state should always be open, but got: {newState}");
            }
            else if (oldState.IsOpen)
            {
                Assert.True(newState.IsHalfOpen, $"when the message pump comes from an open state, the next state should always be half-open, but got: {newState}");
            }
            else if (oldState.IsHalfOpen)
            {
                Assert.True(newState.IsClosed, $"when the message pump comes from a half-open state, the next state should always be closed, but got: {newState}");
            }
        }
    }
}
