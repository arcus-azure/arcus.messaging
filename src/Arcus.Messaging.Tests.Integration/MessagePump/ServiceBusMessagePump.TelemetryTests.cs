using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Arcus.Testing;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Xunit;
using Xunit.Sdk;
using static Arcus.Observability.Telemetry.Core.ContextProperties.RequestTracking.ServiceBus;
using static Microsoft.Extensions.Logging.ServiceBusEntityType;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        private const string DefaultSqlTable = "master";

        private string CustomOperationName { get; } = $"operation-{Guid.NewGuid()}";
        private bool IsSuccessful { get; } = Bogus.Random.Bool();

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormatNewParentViaOpenTelemetry_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var options = new WorkerOptions();
            using ActivitySource source = CreateActivitySource();

            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), pump =>
            {
                pump.Telemetry.OperationName = CustomOperationName;

            }).UseServiceBusOpenTelemetryRequestTracking(source)
              .WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>(CreateAutoTrackingMessageHandler);

            var activities = new Collection<Activity>();
            options.AddOpenTelemetry()
                   .WithTracing(traces =>
                   {
                       traces.AddSource(source.Name);
                       traces.AddInMemoryExporter(activities);
                       traces.AddSqlClientInstrumentation();
                       traces.SetSampler(new AlwaysOnSampler());
                   }); 

            ServiceBusMessage message = CreateOrderServiceBusMessage();

            // Act
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                Activity serviceBusRequest = await GetQueueRequestActivityAsync(activities, CustomOperationName);
                Activity sqlDependency = await GetDependencyActivityAsync(activities, DefaultSqlTable, a => a.ParentId == serviceBusRequest.Id);

                Assert.Equal(serviceBusRequest, sqlDependency.Parent);
            });
        }

        private static ServiceBusMessage CreateOrderServiceBusMessage()
        {
            return new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(OrderGenerator.Generate())));
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormatViaOpenTelemetry_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var options = new WorkerOptions();
            using ActivitySource source = CreateActivitySource();

            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), pump =>
            {
                pump.Telemetry.OperationName = CustomOperationName;

            }).WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>(CreateAutoTrackingMessageHandler)
              .UseServiceBusOpenTelemetryRequestTracking(source);

            var activities = new Collection<Activity>();
            options.AddOpenTelemetry()
                   .WithTracing(traces =>
                   {
                       traces.AddSource(source.Name);
                       traces.AddInMemoryExporter(activities);
                       traces.AddSqlClientInstrumentation();
                       traces.SetSampler(new AlwaysOnSampler());
                   });

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();

                Activity serviceBusRequest = await GetQueueRequestActivityAsync(activities, CustomOperationName, a => a.TraceId.ToString() == transactionId && a.ParentSpanId.ToString() == operationParentId);
                Activity sqlDependency = await GetDependencyActivityAsync(activities, DefaultSqlTable, a => a.TraceId.ToString() == transactionId && a.ParentId == serviceBusRequest.Id);

                Assert.Equal(serviceBusRequest, sqlDependency.Parent);
            });
        }

        private OrderWithAutoTrackingAzureServiceBusMessageHandler CreateAutoTrackingMessageHandler(IServiceProvider provider)
        {
            return new OrderWithAutoTrackingAzureServiceBusMessageHandler(
                IsSuccessful,
                provider.GetRequiredService<ILogger<OrderWithAutoTrackingAzureServiceBusMessageHandler>>());
        }

        private static ActivitySource CreateActivitySource()
        {
            return new ActivitySource("Arcus.Messaging.Tests.Integration");
        }

        private async Task<Activity> GetQueueRequestActivityAsync(IReadOnlyCollection<Activity> activities, string operationName, Func<Activity, bool> filter = null)
        {
            return await Poll.Target<Activity, XunitException>(() =>
            {
                Assert.NotEmpty(activities);
                return AssertX.Any(activities.Where(a => a.OperationName == operationName), request =>
                {
                    Assert.True(IsSuccessful == request.Status is ActivityStatusCode.Ok, $"request for operation '{operationName}' did not match the expected status, expected '{(IsSuccessful ? ActivityStatusCode.Ok : ActivityStatusCode.Error)}' but got '{request.Status}'");
                    Assert.Contains(request.Tags, tag => tag is { Key: "ServiceBus-EntityType", Value: "Queue" });
                    Assert.True(filter is null || filter(request), $"request for operation '{operationName}' did not match the given custom filter assertion, please check whether the OpenTelemetry correlation system did add all the necessary properties");
                });

            }).FailWith("cannot find request telemetry in spied-upon OpenTelemetry activities");
        }

        private static async Task<Activity> GetDependencyActivityAsync(IReadOnlyCollection<Activity> activities, string operationName, Func<Activity, bool> filter = null)
        {
            return await Poll.Target<Activity, XunitException>(() =>
            {
                Assert.NotEmpty(activities);
                return AssertX.Any(activities.Where(a => a.OperationName == operationName), dependency =>
                {
                    Assert.True(filter is null || filter(dependency), $"dependency for operation '{operationName}' did not match the given custom filter assertion, please check whether the OpenTelemetry correlation system did add all the necessary properties");
                });

            }).FailWith("cannot find dependency telemetry in spied-upon OpenTelemetry activities");
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormat_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var options = new WorkerOptions();

            string operationName = Guid.NewGuid().ToString();
            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), configureMessagePump: opt => 
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
            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), configureMessagePump: opt =>
            {
                opt.Routing.Telemetry.OperationName = operationName;
                opt.AutoComplete = true;

            }).WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>();
            
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();
            WithTelemetryConverter(options, spySink);
            WithTelemetryChannel(options, spyChannel);

            var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(OrderGenerator.Generate()));

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
