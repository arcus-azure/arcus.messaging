using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Arcus.Testing;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Xunit;
using static Arcus.Observability.Telemetry.Core.ContextProperties.RequestTracking.ServiceBus;
using static Microsoft.Extensions.Logging.ServiceBusEntityType;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormat_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();

            string customOperationName = Guid.NewGuid().ToString();
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump(pump => pump.Telemetry.OperationName = customOperationName)
                      .WithMatchedServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler>();

            WithTelemetryChannel(serviceBus.Services, spyChannel);
            WithTelemetryConverter(serviceBus.Services, spySink);

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync(1);

            // Assert
            string transactionId = Assert.Single(messages).ApplicationProperties.GetTraceParent().transactionId;

            RequestTelemetry requestViaArcusServiceBus = await GetTelemetryRequestAsync(spySink, customOperationName, r => r.Context.Operation.Id == transactionId);
            DependencyTelemetry dependencyViaArcusKeyVault = await GetTelemetryDependencyAsync(spySink, "Azure key vault", d => d.Context.Operation.Id == transactionId);
            DependencyTelemetry dependencyViaMicrosoftSql = await GetTelemetryDependencyAsync(spyChannel, "SQL", d => d.Context.Operation.Id == transactionId);

            Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
            Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormatForNewParent_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();

            string customOperationName = Guid.NewGuid().ToString();
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump(pump => pump.Telemetry.OperationName = customOperationName)
                      .WithMatchedServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler>();

            WithTelemetryChannel(serviceBus.Services, spyChannel);
            WithTelemetryConverter(serviceBus.Services, spySink);

            // Act
            await serviceBus.WhenProducingMessagesAsync(msg => msg.WithoutTraceParent());

            // Assert
            RequestTelemetry requestViaArcusServiceBus = await GetTelemetryRequestAsync(spySink, customOperationName);
            DependencyTelemetry dependencyViaArcusKeyVault = await GetTelemetryDependencyAsync(spySink, "Azure key vault");
            DependencyTelemetry dependencyViaMicrosoftSql = await GetTelemetryDependencyAsync(spyChannel, "SQL");

            Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
            Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
        }

        private static void WithTelemetryChannel(WorkerOptions options, ITelemetryChannel channel)
        {
            options.Services.Configure<TelemetryConfiguration>(conf => conf.TelemetryChannel = channel);
        }

        private static void WithTelemetryConverter(WorkerOptions options, ITelemetryConverter converter)
        {
            options.ConfigureSerilog(config => config.WriteTo.ApplicationInsights(converter));
        }

        private static async Task<RequestTelemetry> GetTelemetryRequestAsync(InMemoryApplicationInsightsTelemetryConverter spySink, string operationName, Func<RequestTelemetry, bool> filter = null)
        {
            return await Poll.Target(() => GetRequestFrom(spySink.Telemetries, r =>
            {
                return r.Name == operationName
                       && r.Properties[EntityType] == Queue.ToString()
                       && (filter is null || filter(r));
            })).FailWith($"cannot find request telemetry in spied-upon Serilog sink with operation name: {operationName}");
        }

        private static async Task<DependencyTelemetry> GetTelemetryDependencyAsync(InMemoryTelemetryChannel spyChannel, string dependencyType, Func<DependencyTelemetry, bool> filter = null)
        {
            return await Poll.Target(() => GetDependencyFrom(spyChannel.Telemetries, d =>
            {
                return d.Type == dependencyType
                       && (filter is null || filter(d));
            })).FailWith($"cannot find dependency telemetry in spied-upon Application Insights channel with dependency type: {dependencyType}");
        }

        private static async Task<DependencyTelemetry> GetTelemetryDependencyAsync(InMemoryApplicationInsightsTelemetryConverter spySink, string dependencyType, Func<DependencyTelemetry, bool> filter = null)
        {
            return await Poll.Target(() => GetDependencyFrom(spySink.Telemetries, d =>
            {
                return d.Type == dependencyType
                       && (filter is null || filter(d));
            })).FailWith($"cannot find dependency telemetry in spied-upon Application Insights channel with dependency type: {dependencyType}");
        }

        private static RequestTelemetry GetRequestFrom(
            IEnumerable<ITelemetry> telemetries,
            Predicate<RequestTelemetry> filter)
        {
            Assert.NotEmpty(telemetries);

            ITelemetry[] result = telemetries.Where(t => t is RequestTelemetry r && filter(r)).ToArray();
            Assert.True(result.Length > 0, "Should find at least a single request telemetry, but got none");

            return (RequestTelemetry) result.First();
        }

        private static DependencyTelemetry GetDependencyFrom(
            IEnumerable<ITelemetry> telemetries,
            Predicate<DependencyTelemetry> filter)
        {
            Assert.NotEmpty(telemetries);

            ITelemetry[] result = telemetries.Where(t => t is DependencyTelemetry r && filter(r)).ToArray();
            Assert.True(result.Length > 0, "Should find at least a single dependency telemetry, but got none");

            return (DependencyTelemetry) result.First();
        }
    }
}
