using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

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
            Guard.NotLessThan(healthTcpPort, 0, nameof(healthTcpPort), "Requires a TCP health port number that's above zero");
            _healthTcpPort = healthTcpPort;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the <see cref="HealthReport"/> from the exposed TCP health probe.
        /// </summary>
        public async Task<HealthReport> GetHealthReportAsync()
        {
            using (var client = new TcpClient())
            {
                _logger.LogTrace("Connecting to the TCP {Address}:{Port}...", LocalAddress, _healthTcpPort);
                await client.ConnectAsync(IPAddress.Parse(LocalAddress), _healthTcpPort);
                _logger.LogTrace("Connected to the TCP {Address}:{Port}", LocalAddress, _healthTcpPort);
                
                _logger.LogTrace("Retrieving health report...");
                using (NetworkStream clientStream = client.GetStream())
                using (var reader = new StreamReader(clientStream))
                {
                    string healthReport = await reader.ReadToEndAsync();
                    var report = JsonConvert.DeserializeObject<HealthReport>(healthReport, new HealthReportEntryConverter());

                    _logger.LogTrace("Health report retrieved");
                    return report;
                }
            }
        }
    }
}
