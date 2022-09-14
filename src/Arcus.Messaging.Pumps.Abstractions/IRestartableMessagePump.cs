using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    /// Represents an <see cref="MessagePump"/> that can be restarted programmatically.
    /// </summary>
    public interface IRestartableMessagePump : IHostedService
    {
        /// <summary>
        /// Gets the unique message pump ID to identify the pump that needs to be restarted.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Programmatically restart the message pump.
        /// </summary>
        /// <param name="cancellationToken">The token to cancel the restart process.</param>
        Task RestartAsync(CancellationToken cancellationToken);
    }
}
