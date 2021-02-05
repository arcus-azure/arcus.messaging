using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Health.Tcp;
using GuardNet;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arcus.Messaging.Health.Publishing
{
    /// <summary>
    /// Represents an <see cref="IHealthCheckPublisher"/> that lets the <see cref="TcpHealthListener"/> accept or reject TCP connections based on the published <see cref="HealthReport"/>.
    /// </summary>
    internal class TcpHealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly TcpHealthListener _healthListener;
        private readonly TcpHealthListenerOptions _options;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthListener"/> class.
        /// </summary>
        /// <param name="healthListener">The registered <see cref="TcpHealthListener"/> instance.</param>
        /// <param name="options">The registered options to configure the <see cref="TcpHealthListener"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="healthListener"/>, or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="options"/> doesn't have a filled-out value.</exception>
        public TcpHealthCheckPublisher(TcpHealthListener healthListener, TcpHealthListenerOptions options)
            : this(healthListener, options, NullLogger<TcpHealthCheckPublisher>.Instance)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthListener"/> class.
        /// </summary>
        /// <param name="healthListener">The registered <see cref="TcpHealthListener"/> instance.</param>
        /// <param name="options">The registered options to configure the <see cref="TcpHealthListener"/>.</param>
        /// <param name="logger">The logger to write diagnostic trace messages during accepting or rejecting TCP connections.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="healthListener"/>, <paramref name="options"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="options"/> doesn't have a filled-out value.</exception>
        public TcpHealthCheckPublisher(TcpHealthListener healthListener, TcpHealthListenerOptions options, ILogger<TcpHealthCheckPublisher> logger)
        {
            Guard.NotNull(healthListener, nameof(healthListener), "Requires a TCP health listener to accept or reject TCP connections");
            Guard.NotNull(options, nameof(options), "Requires a set of registered options to determine if the TCP connections should be accepted or rejected based on the health report");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic trace messages when TCP connections are accepted or rejected");
            
            _healthListener = healthListener;
            _options = options;
            _logger = logger;
        }
        
        /// <summary>
        /// Publishes the provided <paramref name="report" />.
        /// </summary>
        /// <param name="report">The <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport" />. The result of executing a set of health checks.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" />.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> which will complete when publishing is complete.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="report"/> is <c>null</c>.</exception>
        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            Guard.NotNull(report, nameof(report), "Requires a health report to determine whether or not to accept or reject TCP connections");

            if (_options.RejectTcpConnectionWhenUnhealthy)
            {
                _logger.LogTrace("Health checks report is '{Status}'", report.Status);
                if (report.Status is HealthStatus.Unhealthy)
                {
                    if (_healthListener.IsListening)
                    {
                        _logger.LogTrace("Reject TCP connections, stop existing TCP connections because the health check status is 'Unhealthy'");
                        _healthListener.StopListeningForTcpConnections();
                    }
                }
                else
                {
                    if (!_healthListener.IsListening)
                    {
                        _logger.LogTrace("Accept TCP connections because the health check status is not 'Unhealthy' anymore");
                        _healthListener.StartListeningForTcpConnections();
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
