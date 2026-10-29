using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.ServiceBus.Resiliency;
using Arcus.Messaging.Tests.Unit.MessagePump.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessagePump
{
    public class CircuitBreakerStateMachineTests
    {
        private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan NotificationSettleTime = TimeSpan.FromMilliseconds(250);

        [Fact]
        public void Current_WithoutAnyTransition_IsClosed()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();

            // Act / Assert
            Assert.True(machine.Current.IsClosed);
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
            Assert.True(isTransitioned);
            Assert.True(machine.Current.IsOpen);
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
            Assert.False(isTransitioned);
            Assert.True(machine.Current.IsOpen);
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
            Assert.False(isTransitioned);
            Assert.True(machine.Current.IsHalfOpen);
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
            Assert.True(isTransitioned);
            Assert.True(machine.Current.IsHalfOpen);
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
            Assert.False(isTransitioned);
            Assert.True(machine.Current.IsClosed);
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
            Assert.False(isTransitioned);
            Assert.True(machine.Current.IsHalfOpen);
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
            Assert.True(isTransitioned);
            Assert.True(machine.Current.IsClosed);
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
            Assert.True(isTransitioned);
            Assert.True(machine.Current.IsClosed);
        }

        [Fact]
        public void TryClose_WhenAlreadyClosed_IsIgnored()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();

            // Act
            bool isTransitioned = machine.TryClose();

            // Assert
            Assert.False(isTransitioned);
            Assert.True(machine.Current.IsClosed);
        }

        [Fact]
        public void MarkPumpStopped_WhenClosed_Opens()
        {
            // Arrange
            CircuitBreakerStateMachine machine = CreateStateMachine();

            // Act
            machine.MarkPumpStopped();

            // Assert
            Assert.True(machine.Current.IsOpen);
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
            Assert.True(machine.Current.IsClosed);
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

            var machine = new CircuitBreakerStateMachine(jobId, provider, NullLogger.Instance);

            // Act
            machine.TryOpen(new MessagePumpCircuitBreakerOptions());
            machine.TryHalfOpen();
            machine.TryClose();

            // Assert
            await Wait.UntilAsync(
                () => matchingHandler.Transitions.Count >= 3,
                NotificationTimeout,
                () => $"circuit breaker should have notified 3 transitions, but notified: {string.Join(", ", matchingHandler.Transitions)}");

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

            var machine = new CircuitBreakerStateMachine(jobId, CreateServiceProvider((jobId, handler)), NullLogger.Instance);

            // Act
            machine.TryHalfOpen();
            machine.TryClose();

            // Assert
            await Task.Delay(NotificationSettleTime);
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
            Assert.True(machine.Current.IsOpen);
            Assert.Contains(machine.Current.Options, options);
        }

        private static CircuitBreakerStateMachine CreateStateMachine()
        {
            return new CircuitBreakerStateMachine(
                $"job-{Guid.NewGuid()}",
                new ServiceCollection().BuildServiceProvider(),
                NullLogger.Instance);
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
