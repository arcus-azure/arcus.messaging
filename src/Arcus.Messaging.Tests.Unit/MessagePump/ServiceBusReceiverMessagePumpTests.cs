using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Unit.MessagePump.Fixture;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessagePump
{
    public class ServiceBusReceiverMessagePumpTests
    {
        private static readonly TimeSpan PumpActivityTimeout = TimeSpan.FromSeconds(30);

        private readonly ITestOutputHelper _outputWriter;

        public ServiceBusReceiverMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task StartProcessingMessages_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable()
        {
            // Arrange
            var spy = new MessageProcessingSpy { DependencyIsAvailable = false };
            var receiver = new StubServiceBusReceiver(CreateOrderMessage);

            await using MessagePumpHost host = CreateMessagePumpHost(
                receiver,
                spy,
                collection => collection.WithServiceBusMessageHandler<CircuitBreakerOrderMessageHandler, Order>());

            // Act
            await host.StartAsync();

            // Assert
            await Wait.UntilAsync(
                () => !host.CircuitBreakerState.IsClosed,
                PumpActivityTimeout,
                () => "circuit should have opened because the dependency system is unavailable, but remained closed");

            // While the dependency stays unavailable, the pump should keep retrying single messages without ever processing one.
            await spy.ShouldFailAtLeastAsync(3, PumpActivityTimeout);
            Assert.Empty(spy.ProcessedMessageIds);

            // The dependency system heals.
            spy.DependencyIsAvailable = true;

            await spy.ShouldProcessAtLeastAsync(3, PumpActivityTimeout);
            Assert.True(host.CircuitBreakerState.IsClosed, $"circuit should be closed again now that the dependency system is available, but was: {host.CircuitBreakerState}");
        }

        [Fact]
        public async Task StartProcessingMessages_WithUnavailableDependencySystem_NotifiesCircuitBreakerTransitions()
        {
            // Arrange
            var spy = new MessageProcessingSpy { DependencyIsAvailable = false };
            var transitions = new CircuitBreakerTransitionSpy();
            var receiver = new StubServiceBusReceiver(CreateOrderMessage);

            await using MessagePumpHost host = CreateMessagePumpHost(
                receiver,
                spy,
                collection => collection.WithServiceBusMessageHandler<CircuitBreakerOrderMessageHandler, Order>()
                                        .WithCircuitBreakerStateChangedEventHandler(_ => transitions));

            // Act
            await host.StartAsync();

            // Assert
            await spy.ShouldFailAtLeastAsync(2, PumpActivityTimeout);
            spy.DependencyIsAvailable = true;

            await spy.ShouldProcessAtLeastAsync(1, PumpActivityTimeout);

            await Wait.UntilAsync(
                () => transitions.Transitions.Count >= 3,
                PumpActivityTimeout,
                () => $"circuit breaker should have notified the transition back to a closed circuit, but only notified: {string.Join(", ", transitions.Transitions)}");

            // The event handlers are notified in the background, so only the set of transitions is deterministic - not their order.
            Assert.Contains("Closed->Open", transitions.Transitions);
            Assert.Contains("Open->HalfOpen", transitions.Transitions);
            Assert.Contains("HalfOpen->Closed", transitions.Transitions);
        }

        [Fact]
        public async Task StartProcessingMessages_WhenCircuitBreakerOpenedDuringReceive_AbandonsReceivedMessages()
        {
            // Arrange
            var firstMessageIsProcessing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var circuitIsOpened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var spy = new MessageProcessingSpy
            {
                DependencyIsAvailable = false,

                // Let the first (failing) message hang until the pump started its second receive.
                OnProcessingAsync = attempt => attempt == 1 ? firstMessageIsProcessing.Task : Task.CompletedTask
            };

            var transitions = new CircuitBreakerTransitionSpy();
            var receiver = new StubServiceBusReceiver(CreateOrderMessage)
            {
                // Only hand out a single message per receive, so the pump always has capacity left to receive again.
                MessagesPerBatch = 1,

                // Only let the second receive complete once the first message opened the circuit,
                // so that its messages are guaranteed to arrive on an already-opened circuit.
                OnReceivingBatchAsync = (batchIndex, cancellationToken) =>
                {
                    if (batchIndex != 2)
                    {
                        return Task.CompletedTask;
                    }

                    firstMessageIsProcessing.TrySetResult();
                    return circuitIsOpened.Task.WaitAsync(cancellationToken);
                }
            };

            transitions.OnTransitioned = transition =>
            {
                if (transition is "Closed->Open")
                {
                    circuitIsOpened.TrySetResult();
                }
            };

            await using MessagePumpHost host = CreateMessagePumpHost(
                receiver,
                spy,
                collection => collection.WithServiceBusMessageHandler<CircuitBreakerOrderMessageHandler, Order>()
                                        .WithCircuitBreakerStateChangedEventHandler(_ => transitions),
                options => options.MaxMessagesPerBatch = 2);

            // Act
            await host.StartAsync();

            // Assert
            await Wait.UntilAsync(
                () => receiver.MessageIdsOfBatch(2).Count > 0
                      && receiver.MessageIdsOfBatch(2).All(receiver.AbandonedMessageIds.Contains),
                PumpActivityTimeout,
                () => $"message pump should have abandoned the messages [{string.Join(", ", receiver.MessageIdsOfBatch(2))}] that were received while the circuit was open, but only abandoned [{string.Join(", ", receiver.AbandonedMessageIds)}]");

            Assert.NotEmpty(receiver.MessageIdsOfBatch(2));
            Assert.All(receiver.MessageIdsOfBatch(2), messageId => Assert.Contains(messageId, receiver.AbandonedMessageIds));
            Assert.DoesNotContain(receiver.MessageIdsOfBatch(2), receiver.CompletedMessageIds.Contains);
        }

        [Fact]
        public async Task StartProcessingMessages_WithMaxMessagesPerBatch_NeverProcessesMoreMessagesConcurrently()
        {
            // Arrange
            const int maxMessagesPerBatch = 5;

            var spy = new MessageProcessingSpy { ProcessingDuration = TimeSpan.FromMilliseconds(50) };
            var receiver = new StubServiceBusReceiver(CreateOrderMessage) { MessagesPerBatch = maxMessagesPerBatch };

            await using MessagePumpHost host = CreateMessagePumpHost(
                receiver,
                spy,
                collection => collection.WithServiceBusMessageHandler<TrackingOrderMessageHandler, Order>(),
                options => options.MaxMessagesPerBatch = maxMessagesPerBatch);

            // Act
            await host.StartAsync();

            // Assert
            await spy.ShouldProcessAtLeastAsync(maxMessagesPerBatch * 4, PumpActivityTimeout);

            Assert.InRange(spy.MaxConcurrentHandlers, 2, maxMessagesPerBatch);
            Assert.All(receiver.RequestedBatchSizes, batchSize => Assert.InRange(batchSize, 1, maxMessagesPerBatch));
        }

        [Fact]
        public async Task StopProcessingMessages_AfterProcessingMessages_DisposesReceiverAndOpensCircuit()
        {
            // Arrange
            var spy = new MessageProcessingSpy();
            var receiver = new StubServiceBusReceiver(CreateOrderMessage);

            await using MessagePumpHost host = CreateMessagePumpHost(
                receiver,
                spy,
                collection => collection.WithServiceBusMessageHandler<TrackingOrderMessageHandler, Order>());

            await host.StartAsync();
            await spy.ShouldProcessAtLeastAsync(1, PumpActivityTimeout);

            // Act
            await host.StopAsync();

            // Assert
            Assert.True(receiver.IsDisposed, "message pump should have disposed the Azure Service Bus receiver upon stopping");
            Assert.True(host.CircuitBreakerState.IsOpen, $"circuit should be open as the pump is no longer retrieving messages, but was: {host.CircuitBreakerState}");
            Assert.NotEmpty(receiver.CompletedMessageIds);
        }

        private static ServiceBusReceivedMessage CreateOrderMessage()
        {
            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                BinaryData.FromObjectAsJson(OrderGenerator.Generate()),
                messageId: $"message-{Guid.NewGuid()}");
        }

        private MessagePumpHost CreateMessagePumpHost(
            StubServiceBusReceiver receiver,
            MessageProcessingSpy spy,
            Action<ServiceBusMessageHandlerCollection> configureHandlers,
            Action<ServiceBusMessagePumpOptions> configureOptions = null)
        {
            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddXunitTestLogging(_outputWriter);
            });

            services.AddSingleton(spy);
            services.AddSingleton(new MessagePumpCircuitBreakerOptions
            {
                MessageRecoveryPeriod = TimeSpan.FromMilliseconds(100),
                MessageIntervalDuringRecovery = TimeSpan.FromMilliseconds(50)
            });

            ServiceBusMessageHandlerCollection collection =
                services.AddServiceBusQueueMessagePump(
                    $"queue-{Guid.NewGuid()}",
                    _ => new StubServiceBusClient(receiver),
                    options =>
                    {
                        options.JobId = $"job-{Guid.NewGuid()}";
                        configureOptions?.Invoke(options);
                    });

            configureHandlers(collection);

            return new MessagePumpHost(services.BuildServiceProvider(), collection.JobId);
        }

        private sealed class MessagePumpHost : IAsyncDisposable
        {
            private readonly ServiceProvider _provider;
            private readonly IHostedService _pump;
            private readonly string _jobId;

            internal MessagePumpHost(ServiceProvider provider, string jobId)
            {
                _provider = provider;
                _jobId = jobId;
                _pump = provider.GetServices<IHostedService>().Single();
            }

            internal MessagePumpCircuitState CircuitBreakerState =>
                _provider.GetRequiredService<IMessagePumpCircuitBreaker>().GetCircuitBreakerState(_jobId);

            internal Task StartAsync() => _pump.StartAsync(CancellationToken.None);

            internal Task StopAsync() => _pump.StopAsync(CancellationToken.None);

            public async ValueTask DisposeAsync()
            {
                await _pump.StopAsync(CancellationToken.None);
                await _provider.DisposeAsync();
            }
        }
    }
}
