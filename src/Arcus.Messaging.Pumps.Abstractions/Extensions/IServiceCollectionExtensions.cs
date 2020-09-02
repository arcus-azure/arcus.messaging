using System;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.Messaging.Pumps.Abstractions.Extensions
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> related to message pumps.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithFallbackMessageHandler<TMessageHandler>(this IServiceCollection services)
            where TMessageHandler : class, IFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the fallback message handler to");

            return services.AddSingleton<IFallbackMessageHandler, TMessageHandler>();
        }

        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <param name="createImplementation">The function to create the fallback message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static IServiceCollection WithFallbackMessageHandler<TMessageHandler>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> createImplementation)
            where TMessageHandler : class, IFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the fallback message handler to");
            Guard.NotNull(createImplementation, nameof(createImplementation), "Requires a function to create the fallback message handler");

            return services.AddSingleton<IFallbackMessageHandler, TMessageHandler>(createImplementation);
        }
    }
}
