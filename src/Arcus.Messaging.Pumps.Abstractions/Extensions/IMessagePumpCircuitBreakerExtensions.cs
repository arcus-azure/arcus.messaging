using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Pumps.Abstractions.Resiliency
{
    /// <summary>
    /// Extensions on the <see cref="IMessagePumpCircuitBreaker"/> for more dev-friendly interaction.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IMessagePumpCircuitBreakerExtensions
    {
        /// <summary>
        /// Pause the process of receiving messages in the message pump for a period of time before careful retrying again.
        /// </summary>
        /// <param name="circuitBreaker">The instance to interact with.</param>
        /// <param name="jobId">The unique identifier to distinguish the message pump in the application services.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public static async Task PauseMessageProcessingAsync(
            this IMessagePumpCircuitBreaker circuitBreaker,
            string jobId)
        {
            if (circuitBreaker is null)
            {
                throw new ArgumentNullException(nameof(circuitBreaker));
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank unique job ID to identify he message pump", nameof(jobId));
            }

            await circuitBreaker.PauseMessageProcessingAsync(jobId, _ => { });
        }
    }
}
