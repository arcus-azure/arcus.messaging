using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers;
using Azure.Messaging.EventHubs;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class EventHubsMessagePumpTests
    {
        [Fact]
        public async Task EventHubsMessagePump_PublishMessageForHierarchical_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options, opt => opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical)
                    .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();
            }, MessageCorrelationFormat.Hierarchical);
        }

        [Fact]
        public async Task EventHubsMessagePump_PublishMessageForW3C_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options, opt => opt.Routing.Correlation.Format = MessageCorrelationFormat.W3C)
                    .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();
            });
        }

        [Fact]
        public async Task EventHubsMessagePump_WithCustomTransactionIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            var options = new WorkerOptions();
            var customTransactionIdPropertyName = "MyTransactionId";
            AddEventHubsMessagePump(options, opt =>
                {
                    opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                    opt.Routing.Correlation.TransactionIdPropertyName = customTransactionIdPropertyName;
                })
                .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();

            EventData message = CreateSensorEventDataForHierarchical(transactionIdPropertyName: customTransactionIdPropertyName);

            // Act
            await TestEventHubsMessageHandlingAsync(options, message, async () =>
            {
                // Assert
                SensorReadEventData actual = await DiskMessageEventConsumer.ConsumeSensorReadAsync(message.MessageId);
                AssertReceivedSensorEventDataForHierarchical(message, actual, transactionIdPropertyName: customTransactionIdPropertyName);
            });
        }

        [Fact]
        public async Task EventHubsMessagePump_WithCustomOperationParentIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            var customOperationParentIdPropertyName = "MyOperationParentId";
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, opt =>
                {
                    opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                    opt.Routing.Correlation.OperationParentIdPropertyName = customOperationParentIdPropertyName;
                })
                .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();

            EventData message = CreateSensorEventDataForHierarchical(operationParentIdPropertyName: customOperationParentIdPropertyName);

            // Act
            await TestEventHubsMessageHandlingAsync(options, message, async () =>
            {
                // Assert
                SensorReadEventData actual = await DiskMessageEventConsumer.ConsumeSensorReadAsync(message.MessageId);
                AssertReceivedSensorEventDataForHierarchical(message, actual, operationParentIdPropertyName: customOperationParentIdPropertyName);
            });
        }

         [Fact]
        public async Task EventHubsMessagePump_WithW3CCorrelationFormat_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var spySink = new InMemoryApplicationInsightsTelemetryConverter(_logger);
            var spyChannel = new InMemoryTelemetryChannel(_logger);

            var options = new WorkerOptions();
            options.ConfigureSerilog(config => config.WriteTo.ApplicationInsights(spySink));

            var traceParent = TraceParent.Generate();
            EventData eventData = CreateSensorEventDataForW3C(traceParent: traceParent);

            string operationName = $"operation-{Guid.NewGuid()}";
            AddEventHubsMessagePump(options, opt => opt.Routing.Telemetry.OperationName = operationName)
                .WithEventHubsMessageHandler<SensorReadingAutoTrackingEventHubsMessageHandler, SensorReading>(
                    messageBodyFilter: msg => msg.SensorId == eventData.MessageId);

            TestEventHubsMessageProducer producer = CreateEventHubsMessageProducer();

            await using var worker = await Worker.StartNewAsync(options);
            worker.Services.GetRequiredService<TelemetryConfiguration>().TelemetryChannel = spyChannel;

            // Act
            await producer.ProduceAsync(eventData);

            // Assert
            AssertX.RetryAssertUntil(() =>
            {
                RequestTelemetry requestViaArcusEventHubs = AssertX.GetRequestFrom(spySink.Telemetries, r => r.Name == operationName && r.Context.Operation.Id == traceParent.TransactionId);
                DependencyTelemetry dependencyViaArcusKeyVault = AssertX.GetDependencyFrom(spySink.Telemetries, d => d.Type == "Azure key vault" && d.Context.Operation.Id == traceParent.TransactionId);
                DependencyTelemetry dependencyViaMicrosoftSql = AssertX.GetDependencyFrom(spyChannel.Telemetries, d => d.Type == "SQL" && d.Context.Operation.Id == traceParent.TransactionId);
                    
                Assert.Equal(requestViaArcusEventHubs.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
                Assert.Equal(requestViaArcusEventHubs.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
            }, timeout: TimeSpan.FromMinutes(2), _logger);
        }

        [Fact]
        public async Task EventHubsMessagePump_WithW3CCorrelationFormatForNewParent_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();

            var options = new WorkerOptions();
            options.ConfigureSerilog(config => config.WriteTo.ApplicationInsights(spySink));

            string operationName = Guid.NewGuid().ToString();
            AddEventHubsMessagePump(options, opt => opt.Routing.Telemetry.OperationName = operationName)
                .WithEventHubsMessageHandler<SensorReadingAutoTrackingEventHubsMessageHandler, SensorReading>();

            EventData eventData = CreateSensorEventDataForW3C(traceParent: null);
            TestEventHubsMessageProducer producer = CreateEventHubsMessageProducer();

            await using (var worker = await Worker.StartNewAsync(options))
            {
                worker.Services.GetRequiredService<TelemetryConfiguration>().TelemetryChannel = spyChannel;

                // Act
                await producer.ProduceAsync(eventData);

                // Assert
                AssertX.RetryAssertUntil(() =>
                {
                    IEnumerable<DependencyTelemetry> dependenciesViaArcusKeyVault = spySink.Telemetries.OfType<DependencyTelemetry>().Where(d => d.Type == "Azure key vault");
                    IEnumerable<DependencyTelemetry> dependenciesViaMicrosoftSql = spyChannel.Telemetries.OfType<DependencyTelemetry>().Where(d => d.Type == "SQL");

                    bool correlationSuccess = spySink.Telemetries.Any(t =>
                    {
                        return t is RequestTelemetry r && r.Name == operationName 
                               && dependenciesViaArcusKeyVault.SingleOrDefault(d => d.Context.Operation.ParentId == r.Id) != null
                               && dependenciesViaMicrosoftSql.SingleOrDefault(d => d.Context.Operation.ParentId == r.Id) != null;
                    });
                    Assert.True(correlationSuccess);
                }, timeout: TimeSpan.FromMinutes(1), _logger);
            }
        }
    }
}
