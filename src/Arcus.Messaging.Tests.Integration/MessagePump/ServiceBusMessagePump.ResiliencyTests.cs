using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Resiliency;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Arcus.Messaging.Tests.Integration.MessagePump.TestUnavailableDependencyAzureServiceBusMessageHandler;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusMessageQueuePump_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable()
        {
            // Arrange
            var messageSink = new OrderMessageSink();
            var mockEventHandler1 = new MockCircuitBreakerEventHandler();
            var mockEventHandler2 = new MockCircuitBreakerEventHandler();

            ServiceBusMessage messageBeforeBreak = CreateOrderServiceBusMessageForW3C();
            ServiceBusMessage messageAfterBreak = CreateOrderServiceBusMessageForW3C();

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddSingleton(messageSink)
                   .AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName)
                   .WithServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler, Order>(
                       messageContextFilter: ctx => ctx.MessageId == messageBeforeBreak.MessageId
                                                    || ctx.MessageId == messageAfterBreak.MessageId)
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler1)
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler2);

            var producer = new TestServiceBusMessageProducer(QueueName, _config.GetServiceBus());
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messageBeforeBreak);

            // Assert
            await messageSink.ShouldReceiveOrdersDuringBreakAsync(messageBeforeBreak.MessageId);

            await producer.ProduceAsync(messageAfterBreak);
            await messageSink.ShouldReceiveOrdersAfterBreakAsync(messageAfterBreak.MessageId);

            await mockEventHandler1.ShouldTransitionedCorrectlyAsync();
            await mockEventHandler2.ShouldTransitionedCorrectlyAsync();
        }

        [Fact]
        public async Task ServiceBusMessageTopicPump_WithUnavailableDependencySystem_CircuitBreaksUntilDependencyBecomesAvailable()
        {
            // Arrange
            ServiceBusMessage messageBeforeBreak = CreateOrderServiceBusMessageForW3C();
            ServiceBusMessage messageAfterBreak = CreateOrderServiceBusMessageForW3C();

            var messageSink = new OrderMessageSink();
            var mockEventHandler1 = new MockCircuitBreakerEventHandler();
            var mockEventHandler2 = new MockCircuitBreakerEventHandler();
            await using TemporaryTopicSubscription subscription = await CreateTopicSubscriptionForMessageAsync(messageBeforeBreak, messageAfterBreak);

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .ConfigureSerilog(logging => logging.MinimumLevel.Verbose())
                   .AddSingleton(messageSink)
                   .AddServiceBusTopicMessagePumpUsingManagedIdentity(TopicName, subscription.Name, HostName)
                   .WithServiceBusMessageHandler<TestUnavailableDependencyAzureServiceBusMessageHandler, Order>()
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler1)
                   .WithCircuitBreakerStateChangedEventHandler(_ => mockEventHandler2);

            var producer = new TestServiceBusMessageProducer(TopicName, _config.GetServiceBus());
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messageBeforeBreak);

            // Assert
            await messageSink.ShouldReceiveOrdersDuringBreakAsync(messageBeforeBreak.MessageId);

            await producer.ProduceAsync(messageAfterBreak);
            await messageSink.ShouldReceiveOrdersAfterBreakAsync(messageAfterBreak.MessageId);

            await mockEventHandler1.ShouldTransitionedCorrectlyAsync();
            await mockEventHandler2.ShouldTransitionedCorrectlyAsync();
        }

        private async Task<TemporaryTopicSubscription> CreateTopicSubscriptionForMessageAsync(params ServiceBusMessage[] messages)
        {
            var client = new ServiceBusAdministrationClient(NamespaceConnectionString);
            var sub = await TemporaryTopicSubscription.CreateIfNotExistsAsync(
                client,
                TopicName,
                $"circuit-breaker-{Guid.NewGuid().ToString("N")[..10]}",
                _logger);

            try
            {
                var rule = new CreateRuleOptions("MessageId", new SqlRuleFilter($"sys.messageid in ({string.Join(", ", messages.Select(m => $"'{m.MessageId}'"))})"));
                await client.CreateRuleAsync(TopicName, sub.Name, rule);
            }
            catch (Exception exception) when(exception is ServiceBusException or ArgumentException)
            {
                _logger.LogCritical(exception, "Failed to create a rule on the temporary Azure Topic subscription '{FullyQualifiedNamespace}/{TopicName}/{SubscriptionName}'", sub.FullyQualifiedNamespace, sub.TopicName, sub.Name);
                await sub.DisposeAsync();

                throw;
            }

            return sub;
        }

        [Fact]
        public async Task ServiceBusMessagePump_PauseViaLifetime_RestartsAgain()
        {
            // Arrange
            string jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePumpUsingManagedIdentity(
                       TopicName,
                       subscriptionName: Guid.NewGuid().ToString(),
                       HostName,
                       configureMessagePump: opt =>
                       {
                           opt.JobId = jobId;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            var producer = TestServiceBusMessageProducer.CreateFor(TopicName, _config);
            await using var worker = await Worker.StartNewAsync(options);

            var lifetime = worker.Services.GetRequiredService<IMessagePumpLifetime>();
            await lifetime.PauseProcessingMessagesAsync(jobId, TimeSpan.FromSeconds(5), CancellationToken.None);

            // Act
            await producer.ProduceAsync(message);

            // Assert
            OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId, TimeSpan.FromSeconds(10));
            AssertReceivedOrderEventDataForW3C(message, eventData);
        }
    }

    public class TestUnavailableDependencyAzureServiceBusMessageHandler : CircuitBreakerServiceBusMessageHandler<Order>
    {
        public const int RequiredAttempts = 3;

        private readonly OrderMessageSink _messageSink;

        public TestUnavailableDependencyAzureServiceBusMessageHandler(
            OrderMessageSink messageSink,
            IMessagePumpCircuitBreaker circuitBreaker,
            ILogger<CircuitBreakerServiceBusMessageHandler<Order>> logger) : base(circuitBreaker, logger)
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

        public async Task ShouldReceiveOrdersDuringBreakAsync(string messageId)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts, _orders.Count(o => o.Id == messageId));

            }).Every(_1s).Timeout(_1min).FailWith($"message should be retried {RequiredAttempts} times with the help of the circuit breaker");
        }

        public async Task ShouldReceiveOrdersAfterBreakAsync(string messageId)
        {
            TimeSpan _1s = TimeSpan.FromSeconds(1), _1min = TimeSpan.FromMinutes(1);

            await Poll.Target(() =>
            {
                Assert.Equal(RequiredAttempts + 1, _orders.Count);
                Assert.Single(_orders.Where(o => o.Id == messageId));

            }).Every(_1s).Timeout(_1min).FailWith("pump should continue normal message processing after the message was retried");
        }
    }
}
