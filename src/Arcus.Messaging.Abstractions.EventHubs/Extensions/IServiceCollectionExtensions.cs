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

            return AddEventHubsMessageRouting(services, configureOptions: null);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting(
            this IServiceCollection services,
            Action<AzureEventHubsMessageRouterOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure EventHubs message routing");

            return AddEventHubsMessageRouting(services, (serviceProvider, options) =>
            {
                var logger = serviceProvider.GetService<ILogger<AzureEventHubsMessageRouter>>();
                return new AzureEventHubsMessageRouter(serviceProvider, options, logger);
            }, configureOptions);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureEventHubsMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting<TMessageRouter>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageRouter> implementationFactory)
            where TMessageRouter : IAzureEventHubsMessageRouter
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure EventHubs message routing");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the Azure EventHubs message router");

            return AddEventHubsMessageRouting(services, (serviceProvider, options) => implementationFactory(serviceProvider), configureOptions: null);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureEventHubsMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting<TMessageRouter>(
            this IServiceCollection services,
            Func<IServiceProvider, AzureEventHubsMessageRouterOptions, TMessageRouter> implementationFactory,
            Action<AzureEventHubsMessageRouterOptions> configureOptions)
            where TMessageRouter : IAzureEventHubsMessageRouter
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure EventHubs message routing");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the Azure EventHubs message router");

            services.TryAddSingleton<IAzureEventHubsMessageRouter>(serviceProvider =>
            {
                var options = new AzureEventHubsMessageRouterOptions();
                configureOptions?.Invoke(options);

                return implementationFactory(serviceProvider, options);
            });
            services.AddMessageRouting(serviceProvider => serviceProvider.GetRequiredService<IAzureEventHubsMessageRouter>());

            return new EventHubsMessageHandlerCollection(services);
        }
    }
}
