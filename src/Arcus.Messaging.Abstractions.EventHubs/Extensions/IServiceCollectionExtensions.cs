using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using GuardNet;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureEventHubsMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting(
            this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure EventHubs message routing");

            services.TryAddSingleton<IAzureEventHubsMessageRouter>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<AzureEventHubsMessageRouter>>();
                return new AzureEventHubsMessageRouter(serviceProvider, logger);
            });
            services.AddMessageRouting(serviceProvider => serviceProvider.GetRequiredService<IAzureEventHubsMessageRouter>());

            return new EventHubsMessageHandlerCollection(services);
        }
    }
}
