using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Options to control how the <see cref="TcpHealthListener"/> acts as a TCP endpoint for the <see cref="HealthReport"/>.
    /// </summary>
    public class TcpHealthListenerOptions
    {
        private string _tcpHealthPort;

        /// <summary>
        /// Gets or sets the function that serializes the <see cref="HealthReport"/> to a series of bytes.
        /// </summary>
        public IHealthReportSerializer Serializer { get; set; }

        /// <summary>
        /// Gets or sets the TCP health port on which the health report is exposed.
        /// </summary>
        [Obsolete("Will be removed in v3.0, please use the overload " + nameof(IHealthCheckBuilderExtensions.AddTcpHealthProbe) + " to configure the TCP port used for health checks")]
        public string TcpPortConfigurationKey
        {
            get => _tcpHealthPort;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank configuration key for the TCP port", nameof(value));
                }

                _tcpHealthPort = value;
            }
        }

        /// <summary>
        /// Gets or sets the flag to indicating whether or not the TCP health port should accept or reject TCP connections when a health dependency reports an unhealthy status.
        /// </summary>
        public bool RejectTcpConnectionWhenUnhealthy { get; set; }
    }
}
