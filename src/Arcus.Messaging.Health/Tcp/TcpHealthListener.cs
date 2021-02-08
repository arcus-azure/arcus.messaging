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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Representing a TCP listener as a background process to expose an endpoint where the <see cref="HealthReport"/> is being broadcasted.
    /// </summary>
    public class TcpHealthListener : BackgroundService
    {
        private readonly HealthCheckService _healthService;
        private readonly CustomTcpListener _listener;
        private readonly TcpHealthListenerOptions _tcpListenerOptions;
        private readonly ILogger<TcpHealthListener> _logger;

        private static readonly JsonSerializerSettings SerializationSettings = CreateDefaultSerializerSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthListener"/> class.
        /// </summary>
        /// <param name="configuration">The key-value application configuration properties.</param>
        /// <param name="tcpListenerOptions">The additional options to configure the TCP listener.</param>
        /// <param name="healthService">The service to retrieve the current health of the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/>, <paramref name="tcpListenerOptions"/>, or <paramref name="healthService"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tcpListenerOptions"/> doesn't have a filled-out value.</exception>
        public TcpHealthListener(
            IConfiguration configuration,
            IOptions<TcpHealthListenerOptions> tcpListenerOptions,
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
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tcpListenerOptions"/> doesn't have a filled-out value.</exception>
        public TcpHealthListener(
            IConfiguration configuration,
            IOptions<TcpHealthListenerOptions> tcpListenerOptions,
            HealthCheckService healthService, 
            ILogger<TcpHealthListener> logger)
        {
            Guard.NotNull(tcpListenerOptions, nameof(tcpListenerOptions), "Requires a set of TCP listener options to correctly run the TCP listener");
            Guard.NotNull(healthService, nameof(healthService), "Requires a health service to retrieve the current health status of the application");
            Guard.NotNull(logger, nameof(logger), "Requires a logger implementation to write diagnostic messages during the running of the TCP listener");
            Guard.For(() => tcpListenerOptions.Value is null, new ArgumentException("Requires a set of TCP listener options to correctly run the TCP listener", nameof(tcpListenerOptions)));

            _tcpListenerOptions = tcpListenerOptions.Value;
            _healthService = healthService;
            _logger = logger;

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
        /// Gets the port on which the TCP health server is listening to.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the flag indicating whether or not the TCP probe is listening for client connections.
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
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                HealthReport report = await _healthService.CheckHealthAsync(stoppingToken);
                await AcceptConnectionAsync(report);
            }
        }

        private async Task AcceptConnectionAsync(HealthReport report)
        {
            try
            {
                _logger.LogTrace("Accepting TCP client on port {Port}...", Port);
                using (TcpClient client = await _listener.AcceptTcpClientAsync())
                {
                    _logger.LogTrace("TCP client accepted on port {Port}!", Port);
                    using (NetworkStream clientStream = client.GetStream())
                    {
                        string clientId = client.Client?.RemoteEndPoint?.ToString() ?? string.Empty;
                        _logger.LogTrace("Return '{Status}' health report to client {ClientId}", report.Status, clientId);

                        if (report.Status != HealthStatus.Healthy)
                        {
                            _logger.LogWarning($"Health probe is reporting '{report.Status}' status");
                        }

                        byte[] response = SerializeHealthReport(report);
                        clientStream.Write(response, 0, response.Length);
                    }
                }
            }
            catch (Exception exception) when (exception is ObjectDisposedException || exception is InvalidOperationException)
            {
                _logger.LogTrace("Rejected TCP client on port {Port}", Port);
            }
        }

        private byte[] SerializeHealthReport(HealthReport healthReport)
        {
            IHealthReportSerializer reportSerializer = _tcpListenerOptions.Serializer;
            if (reportSerializer is null)
            {
                string json = JsonConvert.SerializeObject(healthReport, SerializationSettings);
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
