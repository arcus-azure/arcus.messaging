using System;
using System.Threading.Tasks;
using GuardNet;

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
        public static void PauseMessageProcessingAsync(
            this IMessagePumpCircuitBreaker circuitBreaker,
            string jobId)
        {
            Guard.NotNull(circuitBreaker, nameof(circuitBreaker));
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));

            circuitBreaker.PauseMessageProcessingAsync(jobId, _ => { });
        }
    }
}
