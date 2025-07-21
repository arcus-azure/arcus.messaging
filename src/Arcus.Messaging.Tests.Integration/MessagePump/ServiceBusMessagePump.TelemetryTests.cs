using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Arcus.Testing;
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
        private const string DefaultHttpOperationName = "System.Net.Http.HttpRequestOut";

        private string CustomOperationName { get; } = $"operation-{Guid.NewGuid()}";
        private bool IsSuccessful { get; } = Bogus.Random.Bool();

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormatNewParentViaOpenTelemetry_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            using ActivitySource source = CreateActivitySource();
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump(pump =>
            {
                pump.Telemetry.OperationName = CustomOperationName;
            }).UseServiceBusOpenTelemetryRequestTracking(source)
              .WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>(CreateAutoTrackingMessageHandler);

            var activities = new Collection<Activity>();
            serviceBus.Services.AddOpenTelemetry()
                      .WithTracing(traces =>
                      {
                          traces.AddSource(source.Name);
                          traces.AddInMemoryExporter(activities);
                          traces.AddHttpClientInstrumentation();
                          traces.SetSampler(new AlwaysOnSampler());
                      });

            // Act
            await serviceBus.WhenProducingMessagesAsync(msg => msg.WithoutTraceParent());

            // Assert
            Activity serviceBusRequest = await GetQueueRequestActivityAsync(activities, CustomOperationName);
            Activity httpDependency = await GetDependencyActivityAsync(activities, DefaultHttpOperationName, a => a.ParentId == serviceBusRequest.Id);

            Assert.Equal(serviceBusRequest, httpDependency.Parent);
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormatViaOpenTelemetry_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            using ActivitySource source = CreateActivitySource();

            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump(pump =>
            {
                pump.Telemetry.OperationName = CustomOperationName;

            }).WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>(CreateAutoTrackingMessageHandler)
              .UseServiceBusOpenTelemetryRequestTracking(source);

            var activities = new Collection<Activity>();
            serviceBus.Services.AddOpenTelemetry()
                      .WithTracing(traces =>
                      {
                          traces.AddSource(source.Name);
                          traces.AddInMemoryExporter(activities);
                          traces.AddHttpClientInstrumentation();
                          traces.SetSampler(new AlwaysOnSampler());
                      });

            // Act
            ServiceBusMessage message = await serviceBus.WhenProducingMessageAsync();

            // Assert
            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();

            Activity serviceBusRequest = await GetQueueRequestActivityAsync(activities, CustomOperationName, a => a.TraceId.ToString() == transactionId && a.ParentSpanId.ToString() == operationParentId);
            Activity httpDependency = await GetDependencyActivityAsync(activities, DefaultHttpOperationName, a => a.TraceId.ToString() == transactionId && a.ParentId == serviceBusRequest.Id);

            Assert.Equal(serviceBusRequest, httpDependency.Parent);
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
                var requestDependencies = activities.Where(a => a.OperationName == operationName).ToArray();
                Assert.True(requestDependencies.Length > 0,
                    $"no request activities found with operation name '{operationName}' in" +
                    $"[{string.Join(", ", activities.Select(a => a.OperationName))}]");

                return AssertAny(requestDependencies, request =>
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
                var dependencyActivities = activities.Where(a => a.OperationName == operationName).ToArray();
                Assert.True(dependencyActivities.Length > 0,
                    $"no dependency activities found with operation name '{operationName}' in " +
                    $"[{string.Join(", ", activities.Select(a => a.OperationName))}]");

                return AssertAny(dependencyActivities, dependency =>
                {
                    Assert.True(filter is null || filter(dependency), $"dependency for operation '{operationName}' did not match the given custom filter assertion, please check whether the OpenTelemetry correlation system did add all the necessary properties");

                });

            }).FailWith("cannot find dependency telemetry in spied-upon OpenTelemetry activities");
        }

        public static T AssertAny<T>(IEnumerable<T> collection, Action<T> action)
        {
            Stack<(int index, object item, Exception exception)> failures = new();
            T[] array = collection.ToArray();

            for (int index = 0; index < array.Length; ++index)
            {
                T item = array[index];
                try
                {
                    action(item);
                    return item;
                }
                catch (Exception ex)
                {
                    failures.Push((index, item, ex));
                }
            }

            throw new XunitException(
                $"None of the {array.Length} item(s) matches against the given action: {Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, failures.Select(f => $"- [{f.index}] {f.item}: {f.exception}"))}");
        }

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
