using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Messaging.Health;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Testing;
using Bogus;
using Bogus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Health
{
    [Trait("Category", "Integration")]
    public class TcpHealthListenerTests
    {
        [Obsolete("Will be removed in v3.0, once the TCP health probes are not hard-linked to the application configuration anymore")]
        private const string HealthPortConfigurationName = "ARCUS_HEALTH_PORT";
        private const int TcpPort = 5050;

        private readonly TcpHealthService _tcpProbeClient;
        private static readonly Faker Bogus = new();

        public TcpHealthListenerTests(ITestOutputHelper outputWriter)
        {
            _tcpProbeClient = new TcpHealthService(TcpPort, new XunitTestLogger(outputWriter));
        }

        [Fact]
        public async Task TcpHealthProbeWithOrWithoutRejectionOption_AcceptTcpConnection_WhenApplicationIsHealthy()
        {
            // Arrange
            var options = new WorkerOptions();
            string entryName = $"healthy: {Bogus.Lorem.Word()}";
            HealthCheckResult healthy = CreateHealthyResult();
            var possibleSerializer = new EncodedHealthReportSerializer().OrNull(Bogus);

            options.Services.AddHealthChecks()
                            .AddCheck(entryName, () => healthy)
                            .AddTcpHealthProbe(TcpPort, tcp =>
                            {
                                tcp.RejectTcpConnectionWhenUnhealthy = Bogus.Random.Bool();
                                tcp.Serializer = possibleSerializer;
                            });

            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            HealthReport report = await _tcpProbeClient.ShouldReceiveHealthReportAsync(possibleSerializer);
            Assert.Equal(HealthStatus.Healthy, report.Status);
            AssertEqualHealthEntry(entryName, healthy, report);
        }

        [Fact]
        public async Task TcpHealthProbeWithRejectionOption_RejectsTcpConnection_WhenApplicationIsUnhealthy()
        {
            // Arrange
            var options = new WorkerOptions();
            options.Services.AddHealthChecks()
                   .AddCheck("unhealthy", () => HealthCheckResult.Unhealthy())
                   .AddTcpHealthProbe(TcpPort, tcp => tcp.RejectTcpConnectionWhenUnhealthy = true);

            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            await _tcpProbeClient.ShouldRejectHealthReportRequestAsync();
        }

        [Fact]
        public async Task TcpHealthProbe_RemovesExceptionDetails_WhenApplicationUnhealthy()
        {
            // Arrange
            var options = new WorkerOptions();
            string entryName = $"unhealthy: {Bogus.Lorem.Word()}";
            HealthCheckResult unhealthy = CreateUnhealthyResult();
            options.Services.AddHealthChecks()
                   .AddCheck(entryName, () => unhealthy)
                   .AddTcpHealthProbe(TcpPort);

            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            HealthReport report = await _tcpProbeClient.ShouldReceiveHealthReportAsync();

            Assert.Equal(HealthStatus.Unhealthy, report.Status);
            AssertEqualHealthEntry(entryName, unhealthy, report);
        }

        private static void AssertEqualHealthEntry(string expectedEntryName, HealthCheckResult expectedEntry, HealthReport actualReport)
        {
            Assert.Collection(actualReport.Entries, entry =>
            {
                Assert.Equal(expectedEntryName, entry.Key);
                Assert.Equal(expectedEntry.Description, entry.Value.Description);
                Assert.True(expectedEntry.Data.SequenceEqual(entry.Value.Data), "responded TCP health report does not have the same data included");
                Assert.Null(entry.Value.Exception);
            });
        }

        private static HealthCheckResult CreateHealthyResult()
        {
            string description = Bogus.Lorem.Sentence();
            IReadOnlyDictionary<string, object> data =
                Bogus.Make(Bogus.Random.Int(1, 5), () => new KeyValuePair<string, object>(Bogus.Random.Guid().ToString(), Bogus.Lorem.Word()))
                     .ToDictionary(item => item.Key, item => item.Value);

            return HealthCheckResult.Healthy(description, data);
        }

        private static HealthCheckResult CreateUnhealthyResult()
        {
            string description = Bogus.Lorem.Sentence();
            var exception = Bogus.System.Exception();
            IReadOnlyDictionary<string, object> data =
                Bogus.Make(Bogus.Random.Int(1, 5), () => new KeyValuePair<string, object>(Bogus.Random.Guid().ToString(), Bogus.Lorem.Word()))
                     .ToDictionary(item => item.Key, item => item.Value);

            return HealthCheckResult.Unhealthy(description, exception, data);
        }

        [Fact]
        public async Task DeprecatedTcpHealthProbe_AcceptsTcpConnection_WhenHealthCheckIsHealthy()
        {
            // Arrange
            var options = new WorkerOptions();
            options.Configuration.Add(HealthPortConfigurationName, TcpPort.ToString());
            options.Services.AddTcpHealthProbes(HealthPortConfigurationName,
                configureHealthChecks: builder => builder.AddCheck("healhty", () => HealthCheckResult.Healthy()),
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = true);

            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            HealthReport report = await _tcpProbeClient.ShouldReceiveHealthReportAsync();
            Assert.Equal(HealthStatus.Healthy, report.Status);
        }

        [Fact]
        public async Task DeprecatedTcpHealthProbe_RejectsTcpConnection_WhenHealthCheckIsUnhealthy()
        {
            // Arrange
            var options = new WorkerOptions();
            options.Configuration.Add(HealthPortConfigurationName, TcpPort.ToString());
            options.Services.AddTcpHealthProbes(HealthPortConfigurationName,
                configureHealthChecks: builder => builder.AddCheck("unhealhty", () => HealthCheckResult.Unhealthy()),
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = true,
                configureHealthCheckPublisherOptions: opt => opt.Period = TimeSpan.FromSeconds(3));

            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            await _tcpProbeClient.ShouldRejectHealthReportRequestAsync();
        }

        [Fact]
        public async Task DeprecatedTcpHealthProbe_RemovesExceptionDetails_WhenHealthCheckIsUnhealthy()
        {
            // Arrange
            var options = new WorkerOptions();
            options.Configuration.Add(HealthPortConfigurationName, TcpPort.ToString());
            options.Services.AddTcpHealthProbes(HealthPortConfigurationName,
                configureHealthChecks: builder =>
                {
                    builder.AddCheck("unhealhty", () => HealthCheckResult.Unhealthy(
                        description: "Something happened!",
                        data: new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { ["Some"] = "Thing" }),
                        exception: new InvalidOperationException("Something happened!")));
                },
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = false,
                configureHealthCheckPublisherOptions: opt => opt.Period = TimeSpan.FromSeconds(3));

            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            HealthReport afterReport = await _tcpProbeClient.ShouldReceiveHealthReportAsync();
            Assert.Equal(HealthStatus.Unhealthy, afterReport.Status);
            Assert.All(afterReport.Entries, item =>
            {
                Assert.NotNull(item.Value.Description);
                Assert.NotEmpty(item.Value.Data);
                Assert.Null(item.Value.Exception);
            });
        }
    }

    public class EncodedHealthReportSerializer : IHealthReportSerializer
    {
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedHealthReportSerializer"/> class.
        /// </summary>
        public EncodedHealthReportSerializer()
        {
            Encoding = Bogus.PickRandom(Encoding.UTF8, Encoding.ASCII, Encoding.Unicode);
        }

        public Encoding Encoding { get; }

        public byte[] Serialize(HealthReport healthReport)
        {
            string json = JsonSerializer.Serialize(healthReport, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            byte[] response = Encoding.GetBytes(json);
            return response;
        }
    }
}
