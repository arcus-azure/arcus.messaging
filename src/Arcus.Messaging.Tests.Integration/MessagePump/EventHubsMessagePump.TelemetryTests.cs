using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers;
using Arcus.Testing;
using Azure.Messaging.EventHubs;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.Fixture.AssertX;

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
            RequestTelemetry requestViaArcusEventHubs =
                await Poll.Target(() => GetRequestFrom(spySink.Telemetries, r => r.Name == operationName))
                          .Until(r => r.Context.Operation.Id == traceParent.TransactionId)
                          .FailWith("missing request telemetry tracking with W3C format in spied sink");

            DependencyTelemetry dependencyViaArcusKeyVault =
                await Poll.Target(() => GetDependencyFrom(spySink.Telemetries, d => d.Type == "Azure key vault"))
                          .Until(d => d.Context.Operation.Id == traceParent.TransactionId)
                          .FailWith("missing Key vault dependency telemetry tracking via Arcus with W3C format in spied sink");

            DependencyTelemetry dependencyViaMicrosoftSql =
                await Poll.Target(() => GetDependencyFrom(spyChannel.Telemetries, d => d.Type == "SQL"))
                          .Until(d => d.Context.Operation.Id == traceParent.TransactionId)
                          .FailWith("missing SQL dependency telemetry tracking via Microsoft with W3C format in spied channel");

            Assert.Equal(requestViaArcusEventHubs.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
            Assert.Equal(requestViaArcusEventHubs.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
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
                RequestTelemetry requestViaArcusEventHubs =
                    await Poll.Target(() => GetRequestFrom(spySink.Telemetries, r => r.Name == operationName))
                              .Timeout(TimeSpan.FromMinutes(2))
                              .FailWith("missing request telemetry with operation name in spied sink");

                await Poll.Target(() => GetDependencyFrom(spySink.Telemetries, d => d.Type == "Azure key vault"))
                          .Until(d => d.Context.Operation.ParentId == requestViaArcusEventHubs.Id)
                          .FailWith("missing Key vault dependency telemetry tracking via Arcus in spied sink");

                await Poll.Target(() => GetDependencyFrom(spyChannel.Telemetries, d => d.Type == "SQL"))
                          .Until(d => d.Context.Operation.ParentId == requestViaArcusEventHubs.Id)
                          .FailWith("missing SQL dependency telemetry racking via Microsoft on spied channel");
            }
        }
    }
}
