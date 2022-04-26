using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Functions.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extensions on the <see cref="IFunctionsHostBuilder"/> to add the Azure Service Bus router.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IFunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting(this IFunctionsHostBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a set of builder to register the Azure Service Bus message routing");

            return AddServiceBusMessageRouting(builder, configureOptions: null);
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting(
            this IFunctionsHostBuilder builder,
            Action<AzureServiceBusMessageRouterOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a set of builder to register the Azure Service Bus message routing");

            return builder.Services.AddServiceBusMessageRouting(configureOptions);
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureServiceBusMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting<TMessageRouter>(
            this IFunctionsHostBuilder builder,
            Func<IServiceProvider, TMessageRouter> implementationFactory)
            where TMessageRouter : IAzureServiceBusMessageRouter
        {
            Guard.NotNull(builder, nameof(builder), "Requires a set of builder to register the Azure Service Bus message routing");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the Azure Service Bus message router");

            return AddServiceBusMessageRouting(builder, (provider, options) => implementationFactory(provider), configureOptions: null);
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureServiceBusMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusMessageRouting<TMessageRouter>(
            this IFunctionsHostBuilder builder,
            Func<IServiceProvider, AzureServiceBusMessageRouterOptions, TMessageRouter> implementationFactory,
            Action<AzureServiceBusMessageRouterOptions> configureOptions)
            where TMessageRouter : IAzureServiceBusMessageRouter
        {
            Guard.NotNull(builder, nameof(builder), "Requires a set of builder to register the Azure Service Bus message routing");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the Azure Service Bus message router");

            return builder.Services.AddServiceBusMessageRouting(implementationFactory, configureOptions);
        }
    }
}
