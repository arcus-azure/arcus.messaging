using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Representing a TCP server as a background process to expose an endpoint where the <see cref="HealthReport"/> is being broadcasted.
    /// </summary>
    public class TcpHealthListener : BackgroundService
    {
        private readonly HealthCheckService _healthService;
        private readonly TcpListener _listener;
        private readonly TcpHealthListenerOptions _tcpListenerOptions;
        private readonly ILogger<TcpHealthListener> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthListener"/> class.
        /// </summary>
        /// <param name="configuration">The key-value application configuration properties.</param>
        /// <param name="tcpListenerOptions">The additional options to configure the TCP listener.</param>
        /// <param name="healthService">The service to retrieve the current health of the application.</param>
        /// <param name="logger">The logging implementation to write diagnostic messages during the running of the TCP listener.</param>
        public TcpHealthListener(
            IConfiguration configuration,
            IOptions<TcpHealthListenerOptions> tcpListenerOptions,
            HealthCheckService healthService, 
            ILogger<TcpHealthListener> logger)
        {
            Guard.NotNull(tcpListenerOptions, nameof(tcpListenerOptions), "Requires a set of TCP listener options to correctly run the TCP listener");
            Guard.NotNull(healthService, nameof(healthService), "Requires a health service to retrieve the current health status of the application");
            Guard.NotNull(logger, nameof(logger), "Requires a logger implementation to write diagnostic messages during the running of the TCP listener");
            Guard.For<ArgumentException>(() => tcpListenerOptions.Value is null, "Requires a set of TCP listener options to correctly run the TCP listener");

            _tcpListenerOptions = tcpListenerOptions.Value;
            _healthService = healthService;
            _logger = logger;

            Port = GetTcpHealthPort(configuration, _tcpListenerOptions.TcpHealthPort);
            _listener = new TcpListener(IPAddress.Any, Port);
        }

        private static int GetTcpHealthPort(IConfiguration configuration, string tcpHealthPortKey)
        {
            string tcpPortString = configuration[tcpHealthPortKey];
            if (!Int32.TryParse(tcpPortString, out int tcpPort))
            {
                throw new ArithmeticException(
                    $"Requires a configuration implementation with a '{tcpHealthPortKey}' key containing a TCP port number");
            }

            return tcpPort;
        }

        /// <summary>
        /// Gets the port on which the TCP health server is listening to.
        /// </summary>
        public int Port { get; }

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
                _logger.LogTrace("Starting TCP server on port {Port}...", Port);
                _listener.Start();
                _logger.LogInformation("TCP server started on port {Port}!", Port);

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
                await AcceptConnectionAsync();
            }
        }

        private async Task AcceptConnectionAsync()
        {
            _logger.LogTrace("Accepting TCP client on port {Port}...", Port);
            using (TcpClient client = await _listener.AcceptTcpClientAsync())
            {
                _logger.LogInformation("TCP client accepted on port {Port}!", Port);
                using (NetworkStream clientStream = client.GetStream())
                {
                    HealthReport report = await _healthService.CheckHealthAsync();
                    string clientId = client.Client?.RemoteEndPoint?.ToString() ?? String.Empty;
                    _logger.LogInformation("Return '{Status}' health report to client {ClientId}", report.Status, clientId);

                    byte[] response = _tcpListenerOptions.ReportSerializer(report);
                    clientStream.Write(response, 0, response.Length);

                    clientStream.Close();
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
                _logger.LogTrace("Stopping TCP server on port {Port}...", Port);
                _listener.Stop();
                _logger.LogInformation("TCP server stopped on port {Port}!", Port);

                return base.StopAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }
    }
}
