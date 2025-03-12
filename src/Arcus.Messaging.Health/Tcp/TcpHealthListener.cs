using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Representing a TCP listener as a background process to expose an endpoint where the <see cref="HealthReport"/> is being broadcast.
    /// </summary>
    public class TcpHealthListener : BackgroundService
    {
        private readonly HealthCheckService _healthService;
        private readonly CustomTcpListener _listener;
        private readonly TcpHealthListenerOptions _tcpListenerOptions;
        private readonly ILogger<TcpHealthListener> _logger;

        private static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthListener"/> class.
        /// </summary>
        /// <param name="configuration">The key-value application configuration properties.</param>
        /// <param name="tcpListenerOptions">The additional options to configure the TCP listener.</param>
        /// <param name="healthService">The service to retrieve the current health of the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/>, <paramref name="tcpListenerOptions"/>, or <paramref name="healthService"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tcpListenerOptions"/> does not have a filled-out value.</exception>
        public TcpHealthListener(
            IConfiguration configuration,
            TcpHealthListenerOptions tcpListenerOptions,
            HealthCheckService healthService)
            : this(configuration, tcpListenerOptions, healthService, NullLogger<TcpHealthListener>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthListener"/> class.
        /// </summary>
        /// <param name="configuration">The key-value application configuration properties.</param>
        /// <param name="tcpListenerOptions">The additional options to configure the TCP listener.</param>
        /// <param name="healthService">The service to retrieve the current health of the application.</param>
        /// <param name="logger">The logging implementation to write diagnostic messages during the running of the TCP listener.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/>, <paramref name="tcpListenerOptions"/>, <paramref name="healthService"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tcpListenerOptions"/> does not have a filled-out value.</exception>
        public TcpHealthListener(
            IConfiguration configuration,
            TcpHealthListenerOptions tcpListenerOptions,
            HealthCheckService healthService,
            ILogger<TcpHealthListener> logger)
        {
            _tcpListenerOptions = tcpListenerOptions ?? throw new ArgumentNullException(nameof(tcpListenerOptions));
            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
            _logger = logger ?? NullLogger<TcpHealthListener>.Instance;

            Port = GetTcpHealthPort(configuration, _tcpListenerOptions.TcpPortConfigurationKey);
            _listener = new CustomTcpListener(IPAddress.Any, Port);
        }

        private static int GetTcpHealthPort(IConfiguration configuration, string tcpHealthPortKey)
        {
            string tcpPortString = configuration[tcpHealthPortKey];
            if (!int.TryParse(tcpPortString, out int tcpPort))
            {
                throw new ArithmeticException($"Requires a configuration implementation with a '{tcpHealthPortKey}' key containing a TCP port number");
            }

            return tcpPort;
        }

        /// <summary>
        /// Gets the port on which the TCP health server is listening to.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the flag indicating whether the TCP probe is listening for client connections.
        /// </summary>
        internal bool IsListening => _listener.Active;

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            StartListeningForTcpConnections();
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Accepts TCP client connections.
        /// </summary>
        internal void StartListeningForTcpConnections()
        {
            _logger.LogTrace("Starting TCP server on port {Port}...", Port);
            _listener.Start();
            _logger.LogTrace("TCP server started on port {Port}", Port);
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long-running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long-running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                HealthReport report = await _healthService.CheckHealthAsync(stoppingToken);
                await AcceptConnectionAsync(report, stoppingToken);
            }
        }

        private async Task AcceptConnectionAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogTrace("Accepting TCP client on port {Port}...", Port);
#if !NETSTANDARD2_1 && !NETCOREAPP
                using (TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken))
#else 
                using (TcpClient client = await _listener.AcceptTcpClientAsync())
#endif
                {
                    _logger.LogTrace("TCP client accepted on port {Port}!", Port);
                    using (NetworkStream clientStream = client.GetStream())
                    {
                        string clientId = client.Client?.RemoteEndPoint?.ToString() ?? string.Empty;
                        _logger.LogTrace("Return '{Status}' health report to client {ClientId}", report.Status, clientId);

                        if (report.Status != HealthStatus.Healthy)
                        {
                            _logger.LogWarning("Health probe is reporting '{Status}' status", report.Status);
                        }

                        byte[] response = SerializeHealthReport(report);
                        await clientStream.WriteAsync(response, 0, response.Length, cancellationToken);
                    }
                }
            }
            catch (Exception exception) when (exception is ObjectDisposedException || exception is InvalidOperationException)
            {
                _logger.LogTrace(exception, "Rejected TCP client on port {Port}", Port);
            }
        }

        private byte[] SerializeHealthReport(HealthReport healthReport)
        {
            IHealthReportSerializer reportSerializer = _tcpListenerOptions.Serializer;
            if (reportSerializer is null)
            {
                HealthReport updatedReport = RemoveExceptionDetails(healthReport);
                string json = JsonSerializer.Serialize(updatedReport, SerializationOptions);
                byte[] response = Encoding.UTF8.GetBytes(json);

                return response;
            }
            else
            {
                byte[] response = reportSerializer.Serialize(healthReport);
                if (response is null)
                {
                    _logger.LogWarning("The custom HealthReport serializer {CustomSerializer}' returned 'null' when serializing the report", reportSerializer.GetType().Name);
                    return Array.Empty<byte>();
                }

                return response;
            }
        }

        private static HealthReport RemoveExceptionDetails(HealthReport report)
        {
            var entries = new Dictionary<string, HealthReportEntry>();
            foreach ((string key, HealthReportEntry entry) in report.Entries)
            {
                entries.Add(key, new HealthReportEntry(entry.Status, entry.Description, entry.Duration, exception: null, entry.Data, entry.Tags));
            }

            return new HealthReport(
                new ReadOnlyDictionary<string, HealthReportEntry>(entries),
                report.TotalDuration);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            StopListeningForTcpConnections();
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Rejects TCP client connections.
        /// </summary>
        internal void StopListeningForTcpConnections()
        {
            _logger.LogTrace("Stopping TCP server on port {Port}...", Port);
            _listener.Stop();
            _logger.LogTrace("TCP server stopped on port {Port}", Port);
        }
    }
}
