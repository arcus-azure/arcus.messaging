using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
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
using ServiceBusEntityType = Arcus.Messaging.Abstractions.ServiceBus.ServiceBusEntityType;

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
            serviceBus.UseSessions = false;

            string messageIdBeforeBreak = $"test-{Bogus.Random.Guid()}",
                   messageIdAfterBreak = $"test-{Bogus.Random.Guid()}";
            var messageSink = WithMessageSink(serviceBus.Services, messageIdBeforeBreak, messageIdAfterBreak);

            serviceBus.WhenServiceBusMessagePump(entityType)
                      .WithMatchedServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler>()
                      .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler1)
                      .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler2);

            ServiceBusMessage messageBeforeBreak = await serviceBus.WhenProducingMessageAsync(msg => msg.WithMessageId(messageIdBeforeBreak));
            await messageSink.ShouldReceiveMessageDuringBreakAsync(messageBeforeBreak);

            // Act
            ServiceBusMessage messageAfterBreak = await serviceBus.WhenProducingMessageAsync(msg => msg.WithMessageId(messageIdAfterBreak));

            // Assert
            await messageSink.ShouldReceiveMessageAfterBreakAsync(messageAfterBreak);

            await mockEventHandler1.ShouldTransitionedCorrectlyAsync();
            await mockEventHandler2.ShouldTransitionedCorrectlyAsync();
        }

        private static OrderMessageSink WithMessageSink(WorkerOptions options, params string[] targetMessageIds)
        {
            var messageSink = new OrderMessageSink(targetMessageIds);
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
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            MessagePumpCircuitBreakerOptions options,
            CancellationToken cancellationToken)
        {
            options.MessageRecoveryPeriod = TimeSpan.FromSeconds(5);
            options.MessageIntervalDuringRecovery = TimeSpan.FromSeconds(1);

            Logger.LogDebug("Process order for the {DeliveryCount}nd time", messageContext.DeliveryCount);
            _messageSink.SendOrder(message, messageContext);

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
        private readonly string[] _targetMessageIds;
        private readonly Collection<(string messageId, Order order)> _messages = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderMessageSink"/> class.
        /// </summary>
        public OrderMessageSink(string[] targetMessageIds)
        {
            _targetMessageIds = targetMessageIds;
        }

        public void SendOrder(Order order, AzureServiceBusMessageContext messageContext)
        {
            if (_targetMessageIds.Contains(messageContext.MessageId))
            {
                _messages.Add((messageContext.MessageId, order));
            }
        }

        public Order[] ReceiveOrders() => _messages.Select(msg => msg.order).ToArray();

        public async Task ShouldReceiveMessageDuringBreakAsync(ServiceBusMessage message)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts, _messages.Count(o => o.messageId == message.MessageId));

            }).Every(_1s).Timeout(_1min).FailWith($"message '{message.MessageId}' should be retried {RequiredAttempts} times with the help of the circuit breaker");
        }

        public async Task ShouldReceiveMessageAfterBreakAsync(ServiceBusMessage message)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts + 1, _messages.Count);
                Assert.Single(_messages, o => o.messageId == message.MessageId);

            }).Every(_1s).Timeout(_1min).FailWith("pump should continue normal message processing after the message was retried");
        }
    }
}
