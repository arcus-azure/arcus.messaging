using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusTopicMessagePump_PauseViaCircuitBreaker_RestartsAgainWithOneMessage()
        {
            // Arrange
            var options = new WorkerOptions();
            ServiceBusMessage[] messages = GenerateShipmentMessages(1);
            TimeSpan recoveryTime = TimeSpan.FromSeconds(10);
            TimeSpan messageInterval = TimeSpan.FromSeconds(2);

            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: "circuit-breaker-" + Guid.NewGuid(),
                       _ => _config.GetServiceBusTopicConnectionString(),
                       opt => opt.TopicSubscription = TopicSubscription.Automatic)
                   .WithServiceBusMessageHandler<TestCircuitBreakerAzureServiceBusMessageHandler, Shipment>(
                        implementationFactory: provider => new TestCircuitBreakerAzureServiceBusMessageHandler(
                            targetMessageIds: messages.Select(m => m.MessageId).ToArray(),
                            configureOptions: opt =>
                            {
                                opt.MessageRecoveryPeriod = recoveryTime;
                                opt.MessageIntervalDuringRecovery = messageInterval;
                            },
                            provider.GetRequiredService<IMessagePumpCircuitBreaker>(),
                            provider.GetRequiredService<ILogger<TestCircuitBreakerAzureServiceBusMessageHandler>>()));

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messages);

            // Assert
            var handler = GetMessageHandler<TestCircuitBreakerAzureServiceBusMessageHandler>(worker);
            AssertX.RetryAssertUntil(() =>
            {
                DateTimeOffset[] arrivals = handler.GetMessageArrivals();

                _outputWriter.WriteLine("Arrivals: {0}", string.Join(", ", arrivals));
                TimeSpan faultMargin = TimeSpan.FromSeconds(1);
                Assert.Collection(arrivals.SkipLast(1).Zip(arrivals.Skip(1)),
                    dates => AssertDateDiff(dates.First, dates.Second, recoveryTime, recoveryTime.Add(faultMargin)),
                    dates => AssertDateDiff(dates.First, dates.Second, messageInterval, messageInterval.Add(faultMargin)));

            }, timeout: TimeSpan.FromMinutes(2), _logger);

            var pump = Assert.IsType<AzureServiceBusMessagePump>(worker.Services.GetService<IHostedService>());
            Assert.True(pump.IsStarted, "pump should be started after circuit breaker scenario");
        }

        private static TMessageHandler GetMessageHandler<TMessageHandler>(Worker worker)
        {
            return Assert.IsType<TMessageHandler>(
                worker.Services.GetRequiredService<MessageHandler>()
                               .GetMessageHandlerInstance());
        }

        private static void AssertDateDiff(DateTimeOffset left, DateTimeOffset right, TimeSpan expectedMin, TimeSpan expectedMax)
        {
            left = new DateTimeOffset(left.Year, left.Month, left.Day, left.Hour, left.Minute, left.Second, 0, left.Offset);
            right = new DateTimeOffset(right.Year, right.Month, right.Day, right.Hour, right.Minute, right.Second, 0, right.Offset);

            TimeSpan actual = right - left;
            Assert.InRange(actual, expectedMin, expectedMax);
        }

        private static ServiceBusMessage[] GenerateShipmentMessages(int count)
        {
            var generator = new Faker<Shipment>()
                .RuleFor(s => s.Id, f => f.Random.Guid().ToString())
                .RuleFor(s => s.Code, f => f.Random.Int(1, 100))
                .RuleFor(s => s.Date, f => f.Date.RecentOffset())
                .RuleFor(s => s.Description, f => f.Lorem.Sentence());

            return Enumerable.Repeat(generator, count).Select(g =>
            {
                Shipment shipment = g.Generate();
                string json = JsonConvert.SerializeObject(shipment);
                return new ServiceBusMessage(json)
                {
                    MessageId = shipment.Id
                };
            }).ToArray();
        }

        [Fact]
        public async Task ServiceBusMessagePump_PauseViaLifetime_RestartsAgain()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            string jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString, 
                       opt =>
                       {
                           opt.JobId = jobId;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using var worker = await Worker.StartNewAsync(options);
            
            var lifetime = worker.Services.GetRequiredService<IMessagePumpLifetime>();
            await lifetime.PauseProcessingMessagesAsync(jobId, TimeSpan.FromSeconds(5), CancellationToken.None);

            // Act
            await producer.ProduceAsync(message);

            // Assert
            OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
            AssertReceivedOrderEventDataForW3C(message, eventData);
        }
    }
}
