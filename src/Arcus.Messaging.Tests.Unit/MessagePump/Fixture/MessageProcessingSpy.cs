using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.MessagePump.Fixture
{
    /// <summary>
    /// Represents the shared state across all message handler instances that the message pump creates,
    /// recording how many messages were processed and how many handlers ran concurrently.
    /// </summary>
    public class MessageProcessingSpy
    {
        private readonly ConcurrentQueue<string> _processedMessageIds = new();
        private int _attempts, _failures, _runningHandlers, _maxRunningHandlers;
        private volatile bool _dependencyIsAvailable = true;

        /// <summary>
        /// Gets or sets the availability of the simulated dependency system that the message handler interacts with.
        /// Processing a message keeps failing for as long as the dependency is unavailable,
        /// and starts succeeding again as soon as the test makes the dependency available.
        /// </summary>
        public bool DependencyIsAvailable
        {
            get => _dependencyIsAvailable;
            set => _dependencyIsAvailable = value;
        }

        /// <summary>
        /// Gets or sets the time that a single message takes to be processed.
        /// </summary>
        public TimeSpan ProcessingDuration { get; init; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the hook that runs upon each processing attempt - before the attempt possibly fails - with the one-based
        /// index of the attempt. Useful to interleave test actions with the pump's message processing.
        /// </summary>
        public Func<int, Task> OnProcessingAsync { get; init; } = _ => Task.CompletedTask;

        /// <summary>
        /// Gets the message IDs that were successfully processed, in order of processing.
        /// </summary>
        public IReadOnlyCollection<string> ProcessedMessageIds => _processedMessageIds.ToArray();

        /// <summary>
        /// Gets the amount of processing attempts that failed.
        /// </summary>
        public int FailureCount => Volatile.Read(ref _failures);

        /// <summary>
        /// Gets the highest amount of message handlers that ran at the same time.
        /// </summary>
        public int MaxConcurrentHandlers => Volatile.Read(ref _maxRunningHandlers);

        internal async Task ProcessAsync(string messageId, CancellationToken cancellationToken)
        {
            TrackMaximum(ref _maxRunningHandlers, Interlocked.Increment(ref _runningHandlers));
            try
            {
                int attempt = Interlocked.Increment(ref _attempts);
                await OnProcessingAsync(attempt);

                if (!DependencyIsAvailable)
                {
                    Interlocked.Increment(ref _failures);
                    throw new InvalidOperationException(
                        $"Cannot process message '{messageId}' (attempt: {attempt}) as the dependency system is unavailable");
                }

                if (ProcessingDuration > TimeSpan.Zero)
                {
                    await Task.Delay(ProcessingDuration, cancellationToken);
                }

                _processedMessageIds.Enqueue(messageId);
            }
            finally
            {
                Interlocked.Decrement(ref _runningHandlers);
            }
        }

        /// <summary>
        /// Waits until at least <paramref name="count"/> messages were successfully processed.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when not enough messages were processed within the <paramref name="timeout"/>.</exception>
        public Task ShouldProcessAtLeastAsync(int count, TimeSpan timeout)
        {
            return Wait.UntilAsync(
                () => _processedMessageIds.Count >= count,
                timeout,
                () => $"message pump should have processed at least {count} message(s) within {timeout:g}, but only processed {_processedMessageIds.Count}");
        }

        /// <summary>
        /// Waits until the message handler failed at least <paramref name="count"/> times on the unavailable dependency system.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when not enough processing attempts failed within the <paramref name="timeout"/>.</exception>
        public Task ShouldFailAtLeastAsync(int count, TimeSpan timeout)
        {
            return Wait.UntilAsync(
                () => FailureCount >= count,
                timeout,
                () => $"message pump should have retried the message at least {count} time(s) within {timeout:g} while the dependency system was unavailable, but only retried {FailureCount} time(s)");
        }

        private static void TrackMaximum(ref int target, int value)
        {
            int current = Volatile.Read(ref target);
            while (value > current)
            {
                int previous = Interlocked.CompareExchange(ref target, value, current);
                if (previous == current)
                {
                    return;
                }

                current = previous;
            }
        }
    }

    /// <summary>
    /// Provides a way to wait for asynchronous message pump activity to settle.
    /// </summary>
    public static class Wait
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(25);

        /// <summary>
        /// Polls until the <paramref name="untilTrue"/> condition is met, failing with the <paramref name="createFailureMessage"/> upon timeout.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when the condition was not met within the <paramref name="timeout"/>.</exception>
        public static async Task UntilAsync(Func<bool> untilTrue, TimeSpan timeout, Func<string> createFailureMessage)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < timeout)
            {
                if (untilTrue())
                {
                    return;
                }

                await Task.Delay(PollingInterval);
            }

            if (!untilTrue())
            {
                throw new TimeoutException(createFailureMessage());
            }
        }
    }

    /// <summary>
    /// Represents an <see cref="IServiceBusMessageHandler{TMessage}"/> implementation that records its processing in a <see cref="MessageProcessingSpy"/>.
    /// </summary>
    public class TrackingOrderMessageHandler : IServiceBusMessageHandler<Order>
    {
        private readonly MessageProcessingSpy _spy;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingOrderMessageHandler" /> class.
        /// </summary>
        public TrackingOrderMessageHandler(MessageProcessingSpy spy)
        {
            _spy = spy;
        }

        public Task ProcessMessageAsync(
            Order message,
            ServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            return _spy.ProcessAsync(messageContext.MessageId, cancellationToken);
        }
    }

    /// <summary>
    ///     Represents a circuit breaker-enhanced message handler that simulates an unavailable dependency system,
    ///     recording its processing in a <see cref="MessageProcessingSpy"/>.
    /// </summary>
    /// <remarks>
    ///     This handler never activates the circuit breaker itself: the <see cref="DefaultCircuitBreakerServiceBusMessageHandler{TMessage}"/>
    ///     base class considers any exception thrown from <see cref="ProcessMessageAsync(Order,ServiceBusMessageContext,MessageCorrelationInfo,MessagePumpCircuitBreakerOptions,CancellationToken)"/>
    ///     a failed interaction with the dependency system, and calls <see cref="IMessagePumpCircuitBreaker.PauseMessageProcessingAsync"/> on our behalf before re-throwing.
    ///     Simply throwing - which this handler does for as long as <see cref="MessageProcessingSpy.DependencyIsAvailable"/> is <c>false</c> - is therefore what opens the circuit.
    /// </remarks>
    public class CircuitBreakerOrderMessageHandler : DefaultCircuitBreakerServiceBusMessageHandler<Order>
    {
        private readonly MessageProcessingSpy _spy;
        private readonly MessagePumpCircuitBreakerOptions _circuitBreakerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOrderMessageHandler" /> class.
        /// </summary>
        public CircuitBreakerOrderMessageHandler(
            MessageProcessingSpy spy,
            MessagePumpCircuitBreakerOptions circuitBreakerOptions,
            IMessagePumpCircuitBreaker circuitBreaker,
            ILogger<CircuitBreakerOrderMessageHandler> logger) : base(circuitBreaker, logger)
        {
            _spy = spy;
            _circuitBreakerOptions = circuitBreakerOptions;
        }

        /// <summary>
        ///     Processes a message by interacting with the simulated dependency system, throwing when that system is unavailable.
        /// </summary>
        /// <remarks>
        ///     The <paramref name="options"/> have to be assigned before interacting with the dependency system,
        ///     as that interaction possibly throws, while the base class only reads these options afterwards to pause the message pump.
        /// </remarks>
        protected override Task ProcessMessageAsync(
            Order message,
            ServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            MessagePumpCircuitBreakerOptions options,
            CancellationToken cancellationToken)
        {
            options.MessageRecoveryPeriod = _circuitBreakerOptions.MessageRecoveryPeriod;
            options.MessageIntervalDuringRecovery = _circuitBreakerOptions.MessageIntervalDuringRecovery;

            return _spy.ProcessAsync(messageContext.MessageId, cancellationToken);
        }
    }

    /// <summary>
    /// Represents an <see cref="ICircuitBreakerEventHandler"/> implementation that records all fired circuit breaker transitions.
    /// </summary>
    public class CircuitBreakerTransitionSpy : ICircuitBreakerEventHandler
    {
        private readonly ConcurrentQueue<string> _transitions = new();

        /// <summary>
        /// Gets or sets the action that runs upon each recorded transition, described as '{OldState}->{NewState}'.
        /// </summary>
        public Action<string> OnTransitioned { get; set; } = _ => { };

        /// <summary>
        /// Gets all recorded transitions, in order of transitioning, described as '{OldState}->{NewState}'.
        /// </summary>
        public IReadOnlyCollection<string> Transitions => _transitions.ToArray();

        public void OnTransition(MessagePumpCircuitStateChangedEventArgs args)
        {
            var transition = $"{args.OldState}->{args.NewState}";

            _transitions.Enqueue(transition);
            OnTransitioned(transition);
        }
    }
}
