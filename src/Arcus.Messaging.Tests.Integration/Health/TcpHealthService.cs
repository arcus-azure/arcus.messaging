using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Integration.Health
{
    /// <summary>
    /// Represents a service to interact with the TCP health probe.
    /// </summary>
    public class TcpHealthService
    {
        private const string LocalAddress = "127.0.0.1";

        private readonly int _healthTcpPort;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthService"/> class.
        /// </summary>
        /// <param name="healthTcpPort">The local health TCP port to contact the TCP health probe.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while interacting with the TCP probe.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="healthTcpPort"/> is not a valid TCP port number.</exception>
        public TcpHealthService(int healthTcpPort, ILogger logger)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(healthTcpPort);
            _healthTcpPort = healthTcpPort;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the <see cref="HealthReport"/> from the exposed TCP health probe.
        /// </summary>
        public async Task<HealthReport> ShouldReceiveHealthReportAsync(EncodedHealthReportSerializer serializer = null)
        {
            return await Poll.Target<HealthReport, SocketException>(() => GetHealthReportFromTcpProbeAsync(serializer?.Encoding))
                             .Until(report => report != null)
                             .Every(TimeSpan.FromSeconds(2))
                             .Timeout(TimeSpan.FromSeconds(30));
        }

        public async Task ShouldRejectHealthReportRequestAsync()
        {
            await Poll.Target<SocketException, ThrowsException>(async () =>
            {
                return await Assert.ThrowsAsync<SocketException>(() => GetHealthReportFromTcpProbeAsync());

            }).Every(TimeSpan.FromSeconds(2))
              .Timeout(TimeSpan.FromSeconds(30));
        }

        private async Task<HealthReport> GetHealthReportFromTcpProbeAsync(Encoding encoding = null)
        {
            using var client = new TcpClient();
            _logger.LogTrace("Connecting to the TCP {Address}:{Port}...", LocalAddress, _healthTcpPort);
            await client.ConnectAsync(IPAddress.Parse(LocalAddress), _healthTcpPort);
            _logger.LogTrace("Connected to the TCP {Address}:{Port}", LocalAddress, _healthTcpPort);

            _logger.LogTrace("Retrieving health report...");
            using NetworkStream clientStream = client.GetStream();
            using var reader = new StreamReader(clientStream, encoding ?? Encoding.UTF8);
            string txt = await reader.ReadToEndAsync();

            JsonElement json = JsonSerializer.Deserialize<JsonElement>(txt);

            if (json.TryGetProperty("entries", out JsonElement entries)
                && json.TryGetProperty("status", out JsonElement status)
                && json.TryGetProperty("totalDuration", out JsonElement totalDuration))
            {
                HealthReport report = ParseHealthReport(entries, status, totalDuration);

                _logger.LogTrace("Health report retrieved");
                return report;
            }

            _logger.LogError("Could not find necessary camelCase health report properties from: {Json}", json);
            return null;
        }

        private static HealthReport ParseHealthReport(JsonElement entries, JsonElement status, JsonElement totalDuration)
        {
            Dictionary<string, HealthReportEntry> reportEntries =
                entries.EnumerateObject()
               .Select(CreateHealthReportEntry)
               .ToDictionary(entry => entry.Key, entry => entry.Value);

            var healthStatus = Enum.Parse<HealthStatus>(status.GetString());
            TimeSpan duration = TimeSpan.Parse(totalDuration.GetString());

            var report = new HealthReport(
                new ReadOnlyDictionary<string, HealthReportEntry>(reportEntries),
                healthStatus,
                duration);

            return report;
        }

        private static KeyValuePair<string, HealthReportEntry> CreateHealthReportEntry(JsonProperty healthEntryJson)
        {
            string name = healthEntryJson.Name;

            JsonElement token = healthEntryJson.Value
                                               .EnumerateArray()
                                               .First();

            var healthStatus = token.GetProperty("status").Deserialize<HealthStatus>();
            var description = token.GetProperty("description").Deserialize<string>();
            var duration = token.GetProperty("duration").Deserialize<TimeSpan>();
            var exception = token.GetProperty("exception").Deserialize<Exception>();
            var data = token.GetProperty("data").Deserialize<Dictionary<string, object>>();
            var tags = token.GetProperty("tags").Deserialize<string[]>();

            var readOnlyDictionary = new ReadOnlyDictionary<string, object>(data ?? []);

            var healthEntry = new HealthReportEntry(healthStatus, description, duration, exception, readOnlyDictionary, tags);
            return new KeyValuePair<string, HealthReportEntry>(name, healthEntry);
        }
    }
}
