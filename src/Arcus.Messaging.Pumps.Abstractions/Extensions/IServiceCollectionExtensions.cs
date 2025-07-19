using System;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds extensions to the <see cref="IServiceCollection"/> for easier message pump registrations
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="ICircuitBreakerEventHandler"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TEventHandler">The custom type of the event handler.</typeparam>
        /// <param name="services">The application services to register the event handler.</param>
        /// <param name="jobId">The unique ID to distinguish the message pump to register this event handler for.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Will be removed in v4.0, please use dedicated extensions related to the message pumps instead")]
        public static IServiceCollection AddCircuitBreakerEventHandler<TEventHandler>(this IServiceCollection services, string jobId)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            return AddCircuitBreakerEventHandler(services, jobId, provider => ActivatorUtilities.CreateInstance<TEventHandler>(provider));
        }

        /// <summary>
        /// Adds an <see cref="ICircuitBreakerEventHandler"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TEventHandler">The custom type of the event handler.</typeparam>
        /// <param name="services">The application services to register the event handler.</param>
        /// <param name="jobId">The unique ID to distinguish the message pump to register this event handler for.</param>
        /// <param name="implementationFactory">The factory function to create the custom <see cref="ICircuitBreakerEventHandler"/> implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Will be removed in v4.0, please use dedicated extensions related to the message pumps instead")]
        public static IServiceCollection AddCircuitBreakerEventHandler<TEventHandler>(
            this IServiceCollection services,
            string jobId,
            Func<IServiceProvider, TEventHandler> implementationFactory)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank job ID to distinguish the message pump for which this circuit breaker event handler is registered", nameof(jobId));
            }

            return services.AddTransient(provider => new CircuitBreakerEventHandler(jobId, implementationFactory(provider)));
        }
    }
}
