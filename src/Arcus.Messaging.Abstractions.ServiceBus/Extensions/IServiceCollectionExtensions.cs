using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Observability.Correlation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as the message pump will be the main point of interaction, instead of these registration - which were only used in Azure Functions applications")]
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting(this IServiceCollection services)
        {
            return AddServiceBusMessageRouting(services, configureOptions: null);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as the message pump will be the main point of interaction, instead of these registration - which were only used in Azure Functions applications")]
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting(
            this IServiceCollection services,
            Action<AzureServiceBusMessageRouterOptions> configureOptions)
        {
            return AddServiceBusMessageRouting(services, serviceProvider =>
            {
                var options = new AzureServiceBusMessageRouterOptions();
                configureOptions?.Invoke(options);
                var logger = serviceProvider.GetService<ILogger<AzureServiceBusMessageRouter>>();

                return new AzureServiceBusMessageRouter(serviceProvider, options, logger);
            });
        }

        /// <summary>
        ///     Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureServiceBusMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as the message pump will be the main point of interaction, instead of these registration - which were only used in Azure Functions applications")]
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting<TMessageRouter>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageRouter> implementationFactory)
            where TMessageRouter : IAzureServiceBusMessageRouter
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            return AddServiceBusMessageRouting(services, (provider, options) => implementationFactory(provider), configureOptions: null);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureServiceBusMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as the message pump will be the main point of interaction, instead of these registration - which were only used in Azure Functions applications")]
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting<TMessageRouter>(
            this IServiceCollection services,
            Func<IServiceProvider, AzureServiceBusMessageRouterOptions, TMessageRouter> implementationFactory,
            Action<AzureServiceBusMessageRouterOptions> configureOptions)
            where TMessageRouter : IAzureServiceBusMessageRouter
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            services.TryAddSingleton<IAzureServiceBusMessageRouter>(serviceProvider =>
            {
                var options = new AzureServiceBusMessageRouterOptions();
                configureOptions?.Invoke(options);

                return implementationFactory(serviceProvider, options);
            });

            services.AddCorrelation<MessageCorrelationInfo>()
                    .AddScoped<IMessageCorrelationInfoAccessor>(serviceProvider =>
                    {
                        return new MessageCorrelationInfoAccessor(
                            serviceProvider.GetRequiredService<ICorrelationInfoAccessor<MessageCorrelationInfo>>());
                    });

            return new ServiceBusMessageHandlerCollection(services);
        }
    }
}
