using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Representing a TCP server as a background process to expose an endpoint where the <see cref="HealthReport"/> is being broadcasted.
    /// </summary>
    public class TcpHealthServer : BackgroundService
    {
        private readonly HealthCheckService _healthService;
        private readonly TcpListener _listener;
        private readonly ILogger<TcpHealthServer> _logger;
        private readonly JsonSerializerSettings _serializerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthServer"/> class.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting settings of the TCP server.</param>
        /// <param name="healthService">The service to retrieve the current health of the application.</param>
        /// <param name="logger">The logging implementation to write diagnostic messages during the running of the TCP server.</param>
        public TcpHealthServer(IConfiguration configuration, HealthCheckService healthService, ILogger<TcpHealthServer> logger)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration implementation to retrieve hosting information for the TCP server");
            Guard.NotNull(healthService, nameof(healthService), "Requires a health service to retrieve the current health status of the application");
            Guard.NotNull(logger, nameof(logger), "Requires a logger implementation to write diagnostic messages during the running of the TCP server");

            _healthService = healthService;
            _logger = logger;

            int tcpPort = GetConfiguredTcpPort(configuration);
            _listener = new TcpListener(IPAddress.Any, tcpPort);
            _serializerSettings = CreateDefaultSerializerSettings();
        }

        private static int GetConfiguredTcpPort(IConfiguration configuration)
        {
            const string arcusTcpPort = "ARCUS_TCP_PORT";
            string tcpPortString = configuration[arcusTcpPort];
            var tcpPort = 0;
            Guard.For<ArgumentException>(
                () => !Int32.TryParse(tcpPortString, out tcpPort), 
                $"Requires a configuration implementation with a '{arcusTcpPort}' key containing a TCP port number");

            return tcpPort;
        }

        private static JsonSerializerSettings CreateDefaultSerializerSettings()
        {
            var serializingSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None, 
                NullValueHandling = NullValueHandling.Ignore
            };

            var enumConverter = new StringEnumConverter { AllowIntegerValues = false };
            serializingSettings.Converters.Add(enumConverter);

            return serializingSettings;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                _logger.LogTrace("Starting TCP server...");
                _listener.Start();
                _logger.LogInformation("TCP server started!");

                return base.StartAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await AcceptConnectionAsync();
            }
        }

        private async Task AcceptConnectionAsync()
        {
            _logger.LogTrace("Accepting TCP client...");
            using (TcpClient client = await _listener.AcceptTcpClientAsync())
            {
                _logger.LogInformation("TCP client accepted!");
                using (NetworkStream ns = client.GetStream())
                {
                    // TODO: something to serialize as BSON instead?
                    HealthReport report = await _healthService.CheckHealthAsync();
                    string serialized = JsonConvert.SerializeObject(report, _serializerSettings);

                    byte[] response = Encoding.UTF8.GetBytes(serialized);
                    ns.Write(response, 0, response.Length);
                    _logger.LogInformation("Return health report: {status}", report.Status);

                    ns.Close();
                    client.Close();
                }
            }
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                _logger.LogTrace("Stopping TCP server...");
                _listener.Stop();
                _logger.LogInformation("TCP server stopped!");

                return base.StopAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }
    }
}
