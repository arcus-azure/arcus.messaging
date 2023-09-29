using System;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using GuardNet;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        /// Adds an <see cref="MessagePump"/> registration to the application services.
        /// </summary>
        /// <typeparam name="TMessagePump">The custom message pump type.</typeparam>
        /// <param name="services">The application services to register the message pump to.</param>
        /// <param name="implementationFactory">The factory function to create the custom <typeparamref name="TMessagePump"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IServiceCollection AddMessagePump<TMessagePump>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessagePump> implementationFactory)
            where TMessagePump : MessagePump
        {
            Guard.NotNull(services, nameof(services), "Requires an application services instance to register the custom message pump instance");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a factory implementation function to create the custom message pump instance");

            services.TryAddSingleton<IMessagePumpLifetime, DefaultMessagePumpLifetime>();
            services.TryAddSingleton<IMessagePumpCircuitBreaker, DefaultMessagePumpCircuitBreaker>();
            return services.AddHostedService(implementationFactory);
        }
    }
}
