using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus.Resiliency;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.TestUnavailableDependencyAzureServiceBusMessageHandler;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Theory]
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public async Task ServiceBusMessagePump_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable(ServiceBusEntityType entityType)
        {
            // Arrange
            var mockEventHandler1 = new MockCircuitBreakerEventHandler();
            var mockEventHandler2 = new MockCircuitBreakerEventHandler();

            await using var serviceBus = GivenServiceBus();

            var messageSink = WithMessageSink(serviceBus.Services);
            serviceBus.WhenServiceBusMessagePump(entityType)
                      .WithMatchedServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler>()
                      .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler1)
                      .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler2);

            ServiceBusMessage messageBeforeBreak = await serviceBus.WhenProducingMessageAsync();
            await messageSink.ShouldReceiveMessageDuringBreakAsync(messageBeforeBreak);

            // Act
            ServiceBusMessage messageAfterBreak = await serviceBus.WhenProducingMessageAsync();

            // Assert
            await messageSink.ShouldReceiveMessageAfterBreakAsync(messageAfterBreak);

            await mockEventHandler1.ShouldTransitionedCorrectlyAsync();
            await mockEventHandler2.ShouldTransitionedCorrectlyAsync();
        }

        private static OrderMessageSink WithMessageSink(WorkerOptions options)
        {
            var messageSink = new OrderMessageSink();
            options.Services.AddSingleton(messageSink);

            return messageSink;
        }
    }

    public class TestUnavailableDependencyAzureServiceBusMessageHandler : CircuitBreakerServiceBusMessageHandler<Order>
    {
        public const int RequiredAttempts = 3;

        private readonly OrderMessageSink _messageSink;

        public TestUnavailableDependencyAzureServiceBusMessageHandler(
            OrderMessageSink messageSink,
            IMessagePumpCircuitBreaker circuitBreaker,
            ILogger<TestUnavailableDependencyAzureServiceBusMessageHandler> logger) : base(circuitBreaker, logger)
        {
            _messageSink = messageSink;
        }

        protected override Task ProcessMessageAsync(
            Order message,
            ServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            MessagePumpCircuitBreakerOptions options,
            CancellationToken cancellationToken)
        {
            options.MessageRecoveryPeriod = TimeSpan.FromSeconds(5);
            options.MessageIntervalDuringRecovery = TimeSpan.FromSeconds(1);

            Logger.LogDebug("Process order for the {DeliveryCount}nd time", messageContext.DeliveryCount);
            _messageSink.SendOrder(message);

            if (_messageSink.ReceiveOrders().Length < RequiredAttempts)
            {
                throw new InvalidOperationException(
                    $"Simulate an unhealthy dependency system! (DeliveryCount: {messageContext.DeliveryCount})");
            }

            return Task.CompletedTask;
        }
    }

    public class OrderMessageSink
    {
        private readonly Collection<Order> _orders = new();

        public void SendOrder(Order message)
        {
            _orders.Add(message);
        }

        public Order[] ReceiveOrders() => _orders.ToArray();

        public async Task ShouldReceiveMessageDuringBreakAsync(ServiceBusMessage message)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts, _orders.Count(o => o.Id == message.MessageId));

            }).Every(_1s).Timeout(_1min).FailWith($"message should be retried {RequiredAttempts} times with the help of the circuit breaker");
        }

        public async Task ShouldReceiveMessageAfterBreakAsync(ServiceBusMessage message)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts + 1, _orders.Count);
                Assert.Single(_orders, o => o.Id == message.MessageId);

            }).Every(_1s).Timeout(_1min).FailWith("pump should continue normal message processing after the message was retried");
        }
    }
}
