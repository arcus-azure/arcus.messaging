using System;
using System.Threading.Tasks;

namespace Arcus.Messaging.Pumps.Abstractions.Resiliency
{
    /// <summary>
    /// Represents an instance to pause the process of receiving messages in the message pump until the message handler can process the messages again.
    /// Usually injected in the message handler to handle transient connection failures with dependencies.
    /// </summary>
    public interface IMessagePumpCircuitBreaker
    {
        /// <summary>
        /// Pause the process of receiving messages in the message pump for a period of time before careful retrying again.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <param name="configureOptions">The optional user-configurable options to manipulate the workings of the message pump interaction.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        Task PauseMessageProcessingAsync(string jobId, Action<MessagePumpCircuitBreakerOptions> configureOptions);

        /// <summary>
        /// Continue the process of receiving messages in the message pump after a successful message handling.
        /// </summary>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        Task ResumeMessageProcessingAsync(string jobId);
    }
}
