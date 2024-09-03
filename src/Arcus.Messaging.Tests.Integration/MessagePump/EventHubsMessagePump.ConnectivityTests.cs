using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.EventHubs;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class EventHubsMessagePumpTests
    {
        [Fact]
        public async Task EventHubsMessagePumpUsingManagedIdentity_PublishesMessage_MessageSuccessfullyProcessed()
        {
            using var auth = TemporaryManagedIdentityConnection.Create(_config, _logger);
            await TestEventHubsMessageHandlingAsync(options =>
            {
                options.AddEventHubsMessagePumpUsingManagedIdentity(
                           eventHubsName: EventHubsName,
                           fullyQualifiedNamespace: FullyQualifiedEventHubsNamespace,
                           blobContainerUri: _blobStorageContainer.ContainerUri,
                           clientId: auth.ClientId)
                       .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();
            });
        }

        [Fact]
        public async Task RestartedEventHubsMessagePump_PublishMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options)
                .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();

            EventData expected = CreateSensorEventDataForW3C();
            TestEventHubsMessageProducer producer = CreateEventHubsMessageProducer();

            await using var worker = await Worker.StartNewAsync(options);
            await using var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger);
            
            IEnumerable<AzureEventHubsMessagePump> messagePumps =
                worker.Services.GetServices<IHostedService>()
                      .OfType<AzureEventHubsMessagePump>();

            AzureEventHubsMessagePump messagePump = Assert.Single(messagePumps);
            Assert.NotNull(messagePump);

            await messagePump.RestartAsync(CancellationToken.None);

            // Act
            await producer.ProduceAsync(expected);

            // Assert
            SensorReadEventData actual = await DiskMessageEventConsumer.ConsumeSensorReadAsync(expected.MessageId);
            AssertReceivedSensorEventDataForW3C(expected, actual);
        }

        [Fact]
        public async Task EventHubsMessagePump_PausesViaLifetime_RestartsAgain()
        {
            // Arrange
            string jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, opt => opt.JobId = jobId)
                .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();

            EventData expected = CreateSensorEventDataForW3C();
            TestEventHubsMessageProducer producer = CreateEventHubsMessageProducer();
            
            await using var worker = await Worker.StartNewAsync(options);
            await using var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger);
            
            var lifetime = worker.Services.GetRequiredService<IMessagePumpLifetime>();
            await lifetime.PauseProcessingMessagesAsync(jobId, TimeSpan.FromSeconds(5), CancellationToken.None);

            // Act
            await producer.ProduceAsync(expected);

            // Assert
            SensorReadEventData actual = await DiskMessageEventConsumer.ConsumeSensorReadAsync(expected.MessageId);
            AssertReceivedSensorEventDataForW3C(expected, actual);
        }
    }
}
