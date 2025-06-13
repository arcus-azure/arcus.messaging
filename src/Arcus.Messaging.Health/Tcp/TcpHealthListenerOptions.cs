using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Options to control how the <see cref="TcpHealthListener"/> acts as a TCP endpoint for the <see cref="HealthReport"/>.
    /// </summary>
    public class TcpHealthListenerOptions
    {
        /// <summary>
        /// Gets or sets the function that serializes the <see cref="HealthReport"/> to a series of bytes.
        /// </summary>
        public IHealthReportSerializer Serializer { get; set; }

        /// <summary>
        /// Gets or sets the flag to indicating whether or not the TCP health port should accept or reject TCP connections when a health dependency reports an unhealthy status.
        /// </summary>
        public bool RejectTcpConnectionWhenUnhealthy { get; set; }
    }
}
