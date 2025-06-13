using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Arcus.Observability.Telemetry.Core.ContextProperties.Correlation;
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
            var options = new WorkerOptions();

            string operationName = Guid.NewGuid().ToString();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => 
            {
                opt.AutoComplete = true;
                opt.Routing.Telemetry.OperationName = operationName;

            }).WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>();
            
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();
            WithTelemetryChannel(options, spyChannel);
            WithTelemetryConverter(options, spySink);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                (string transactionId, string _) = message.ApplicationProperties.GetTraceParent();

                RequestTelemetry requestViaArcusServiceBus = await GetTelemetryRequestAsync(spySink, operationName, r => r.Context.Operation.Id == transactionId);
                DependencyTelemetry dependencyViaArcusKeyVault = await GetTelemetryDependencyAsync(spySink, "Azure key vault", d => d.Context.Operation.Id == transactionId);
                DependencyTelemetry dependencyViaMicrosoftSql = await GetTelemetryDependencyAsync(spyChannel, "SQL", d => d.Context.Operation.Id == transactionId);

                Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
                Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
            });
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormatForNewParent_AutomaticallyTracksMicrosoftDependencies()
        {
             // Arrange
             var options = new WorkerOptions();
            
            string operationName = Guid.NewGuid().ToString();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt =>
            {
                opt.Routing.Telemetry.OperationName = operationName;
                opt.AutoComplete = true;

            }).WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>();
            
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();
            WithTelemetryConverter(options, spySink);
            WithTelemetryChannel(options, spyChannel);

            var message = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                RequestTelemetry requestViaArcusServiceBus = await GetTelemetryRequestAsync(spySink, operationName);
                DependencyTelemetry dependencyViaArcusKeyVault = await GetTelemetryDependencyAsync(spySink, "Azure key vault");
                DependencyTelemetry dependencyViaMicrosoftSql = await GetTelemetryDependencyAsync(spyChannel, "SQL");

                Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
                Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
            });
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
            return await Poll.Target(() => AssertX.GetRequestFrom(spySink.Telemetries, r => 
            {
                return r.Name == operationName 
                       && r.Properties[EntityType] == Queue.ToString() 
                       && (filter is null || filter(r));
            })).FailWith($"cannot find request telemetry in spied-upon Serilog sink with operation name: {operationName}");
        }

        private static async Task<DependencyTelemetry> GetTelemetryDependencyAsync(InMemoryTelemetryChannel spyChannel, string dependencyType, Func<DependencyTelemetry, bool> filter = null)
        {
            return await Poll.Target(() => AssertX.GetDependencyFrom(spyChannel.Telemetries, d =>
            {
                return d.Type == dependencyType 
                       && (filter is null || filter(d));
            })).FailWith($"cannot find dependency telemetry in spied-upon Application Insights channel with dependency type: {dependencyType}");
        }

        private static async Task<DependencyTelemetry> GetTelemetryDependencyAsync(InMemoryApplicationInsightsTelemetryConverter spySink, string dependencyType, Func<DependencyTelemetry, bool> filter = null)
        {
            return await Poll.Target(() => AssertX.GetDependencyFrom(spySink.Telemetries, d =>
            {
                return d.Type == dependencyType 
                       && (filter is null || filter(d));
            })).FailWith($"cannot find dependency telemetry in spied-upon Application Insights channel with dependency type: {dependencyType}");
        }
    }
}
