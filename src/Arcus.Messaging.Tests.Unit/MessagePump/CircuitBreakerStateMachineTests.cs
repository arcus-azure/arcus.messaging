using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.ServiceBus.Resiliency;
using Arcus.Messaging.Tests.Unit.MessagePump.Fixture;
using Arcus.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessagePump
{
    public class CircuitBreakerStateMachineTests
    {
        private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan NotificationSettleTime = TimeSpan.FromMilliseconds(250);

        private readonly ILogger _testLogger;

        public CircuitBreakerStateMachineTests(ITestOutputHelper outputWriter)
        {
            _testLogger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public void Current_WithoutAnyTransition_IsClosed()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();

            // Act / Assert
            Assert.True(machine.Current.IsClosed, "The circuit breaker should be Closed by default.");
        }

        [Fact]
        public void TryOpen_WhenClosed_OpensWithPassedOptions()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            var options = new MessagePumpCircuitBreakerOptions { MessageRecoveryPeriod = TimeSpan.FromSeconds(3) };

            // Act
            bool isTransitioned = machine.TryOpen(options);

            // Assert
            Assert.True(isTransitioned, $"The circuit breaker should have transitioned to another {nameof(MessagePumpCircuitState)}.");
            Assert.True(machine.Current.IsOpen, "The circuit breaker should be Open.");
            Assert.Same(options, machine.Current.Options);
        }

        [Fact]
        public void TryOpen_WhenAlreadyOpen_IsIgnoredAndKeepsOriginalOptions()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            var original = new MessagePumpCircuitBreakerOptions();
            machine.TryOpen(original);

            // Act
            bool isTransitioned = machine.TryOpen(new MessagePumpCircuitBreakerOptions());

            // Assert
            Assert.False(isTransitioned, $"The circuit breaker should not have transitioned to another {nameof(MessagePumpCircuitState)}.");
            Assert.True(machine.Current.IsOpen, "The circuit breaker should be Open.");
            Assert.Same(original, machine.Current.Options);
        }

        [Fact]
        public void TryOpen_WhenHalfOpen_IsIgnoredSoRecoveryKeepsProbing()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            machine.TryOpen(new MessagePumpCircuitBreakerOptions());
            machine.TryHalfOpen();

            // Act
            bool isTransitioned = machine.TryOpen(new MessagePumpCircuitBreakerOptions());

            // Assert
            Assert.False(isTransitioned, $"The circuit breaker cannot transition from HalfOpen to Open.");
            Assert.True(machine.Current.IsHalfOpen, "The circuit breaker should remain in HalfOpen state.");
        }

        [Fact]
        public void TryHalfOpen_WhenOpen_HalfOpensWithSameOptions()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            var options = new MessagePumpCircuitBreakerOptions { MessageIntervalDuringRecovery = TimeSpan.FromSeconds(2) };
            machine.TryOpen(options);

            // Act
            bool isTransitioned = machine.TryHalfOpen();

            // Assert
            Assert.True(isTransitioned, $"The circuit breaker should have transitioned to another {nameof(MessagePumpCircuitState)}.");
            Assert.True(machine.Current.IsHalfOpen, "The circuit breaker should be HalfOpen.");
            Assert.Same(options, machine.Current.Options);
        }

        [Fact]
        public void TryHalfOpen_WhenClosed_IsIgnored()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();

            // Act
            bool isTransitioned = machine.TryHalfOpen();

            // Assert
            Assert.False(isTransitioned, $"The circuit breaker cannot transition from Closed to HalfOpen.");
            Assert.True(machine.Current.IsClosed, "The circuit breaker should be Closed.");
        }

        [Fact]
        public void TryHalfOpen_WhenAlreadyHalfOpen_IsIgnored()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            machine.TryOpen(new MessagePumpCircuitBreakerOptions());
            machine.TryHalfOpen();

            // Act
            bool isTransitioned = machine.TryHalfOpen();

            // Assert
            Assert.False(isTransitioned, "When the current state is HalfOpen, a change to HalfOpen should not be considered as a transition.");
            Assert.True(machine.Current.IsHalfOpen, "The circuit breaker should remain in HalfOpen state.");
        }

        [Fact]
        public void TryClose_WhenOpen_ClosesWithDefaultOptions()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            var options = new MessagePumpCircuitBreakerOptions { MessageRecoveryPeriod = TimeSpan.FromSeconds(5) };
            machine.TryOpen(options);

            // Act
            bool isTransitioned = machine.TryClose();

            // Assert
            Assert.True(isTransitioned, "The circuit breaker should have transitioned from Open to Closed.");
            Assert.True(machine.Current.IsClosed, "The circuit breaker should be Closed.");
            Assert.NotSame(options, machine.Current.Options);
        }

        [Fact]
        public void TryClose_WhenHalfOpen_Closes()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            machine.TryOpen(new MessagePumpCircuitBreakerOptions());
            machine.TryHalfOpen();

            // Act
            bool isTransitioned = machine.TryClose();

            // Assert
            Assert.True(isTransitioned, "The circuit breaker should have transitioned from HalfOpen to Closed.");
            Assert.True(machine.Current.IsClosed, "The circuit breaker should be Closed.");
        }

        [Fact]
        public void TryClose_WhenAlreadyClosed_IsIgnored()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();

            // Act
            bool isTransitioned = machine.TryClose();

            // Assert
            Assert.False(isTransitioned, "When the current state is Closed, a change to Closed should not be considered as a transition.");
            Assert.True(machine.Current.IsClosed, "The circuit breaker should be Closed.");
        }

        [Fact]
        public void MarkPumpStopped_WhenClosed_Opens()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();

            // Act
            machine.MarkPumpStopped();

            // Assert
            Assert.True(machine.Current.IsOpen, "When the pump is marked as 'stopped', the circuit breaker should be Open.");
        }

        [Fact]
        public void MarkPumpStarted_WhenOpen_Closes()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            machine.TryOpen(new MessagePumpCircuitBreakerOptions());

            // Act
            machine.MarkPumpStarted();

            // Assert
            Assert.True(machine.Current.IsClosed, "When the pump is marked as 'started', the circuit breaker should be Closed.");
        }

        [Fact]
        public async Task Transitions_WithRegisteredEventHandler_NotifiesOnlyHandlersOfSameJob()
        {
            // Arrange
            var jobId = $"job-{Guid.NewGuid()}";
            var matchingHandler = new CircuitBreakerTransitionSpy();
            var otherHandler = new CircuitBreakerTransitionSpy();

            IServiceProvider provider = CreateServiceProvider(
                (jobId, matchingHandler),
                ($"job-{Guid.NewGuid()}", otherHandler));

            var machine = new CircuitBreakerStateMachine(jobId, provider, _testLogger);

            // Act
            machine.TryOpen(new MessagePumpCircuitBreakerOptions());
            machine.TryHalfOpen();
            machine.TryClose();

            // Assert
            await Poll.Target(() => matchingHandler.Transitions.Count)
                .Until(numberOfTransitions => numberOfTransitions >= 3)
                .Every(TimeSpan.FromMilliseconds(25))
                .Timeout(NotificationTimeout)
                .FailWith($"circuit breaker should have notified 3 transitions, but notified: {string.Join(", ", matchingHandler.Transitions)}")
                .StartAsync();

            // The handlers are notified in the background, so only the set of transitions is deterministic - not their order.
            Assert.Contains("Closed->Open", matchingHandler.Transitions);
            Assert.Contains("Open->HalfOpen", matchingHandler.Transitions);
            Assert.Contains("HalfOpen->Closed", matchingHandler.Transitions);
            Assert.Empty(otherHandler.Transitions);
        }

        [Fact]
        public async Task TryHalfOpen_WhenClosed_DoesNotNotifyEventHandler()
        {
            // Arrange
            var jobId = $"job-{Guid.NewGuid()}";
            var handler = new CircuitBreakerTransitionSpy();

            var machine = new CircuitBreakerStateMachine(jobId, CreateServiceProvider((jobId, handler)), _testLogger);

            // Act
            machine.TryHalfOpen();
            machine.TryClose();

            // Assert
            await Task.Delay(NotificationSettleTime, TestContext.Current.CancellationToken);
            Assert.Empty(handler.Transitions);
        }

        [Fact]
        public async Task TryOpen_WhenCalledConcurrently_OnlyOpensOnce()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();
            MessagePumpCircuitBreakerOptions[] options =
                Enumerable.Range(0, 50).Select(_ => new MessagePumpCircuitBreakerOptions()).ToArray();

            // Act
            bool[] results = await Task.WhenAll(options.Select(opt => Task.Run(() => machine.TryOpen(opt))));

            // Assert
            Assert.Equal(1, results.Count(isTransitioned => isTransitioned));
            Assert.True(machine.Current.IsOpen, "The circuit breaker should be Open.");
            Assert.Contains(machine.Current.Options, options);
        }

        private CircuitBreakerStateMachine CreateStateMachine()
        {
            return new CircuitBreakerStateMachine(
                $"job-{Guid.NewGuid()}",
                new ServiceCollection().BuildServiceProvider(),
                _testLogger);
        }

        private static IServiceProvider CreateServiceProvider(params (string jobId, ICircuitBreakerEventHandler handler)[] registrations)
        {
            var services = new ServiceCollection();

            foreach ((string jobId, ICircuitBreakerEventHandler handler) in registrations)
            {
                new ServiceBusMessageHandlerCollection(services) { JobId = jobId }
                    .WithCircuitBreakerStateChangedEventHandler(_ => handler);
            }

            return services.BuildServiceProvider();
        }
    }
}
