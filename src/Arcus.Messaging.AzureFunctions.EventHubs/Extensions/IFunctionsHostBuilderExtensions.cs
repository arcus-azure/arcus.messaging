﻿using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.AzureFunctions.EventHubs;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Functions.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IFunctionsHostBuilder"/> related to Azure EventHubs message handling.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IFunctionsHostBuilderExtensions
    {
        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting(
            this IFunctionsHostBuilder builder)
        {
            return AddEventHubsMessageRouting(builder, configureOptions: null);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting(
            this IFunctionsHostBuilder builder,
            Action<AzureEventHubsMessageRouterOptions> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Services.AddSingleton<AzureFunctionsInProcessMessageCorrelation>()
                          .AddEventHubsMessageRouting(configureOptions);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureEventHubsMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting<TMessageRouter>(
            this IFunctionsHostBuilder builder,
            Func<IServiceProvider, TMessageRouter> implementationFactory)
            where TMessageRouter : IAzureEventHubsMessageRouter
        {
            return AddEventHubsMessageRouting(builder, (provider, options) => implementationFactory(provider), configureOptions: null);
        }

        /// <summary>
        ///     Adds an <see cref="IAzureEventHubsMessageRouter"/> implementation
        ///     to route the incoming messages through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="builder">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <param name="configureOptions">The function to configure the options that change the behavior of the router.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureEventHubsMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessageRouting<TMessageRouter>(
            this IFunctionsHostBuilder builder,
            Func<IServiceProvider, AzureEventHubsMessageRouterOptions, TMessageRouter> implementationFactory,
            Action<AzureEventHubsMessageRouterOptions> configureOptions)
            where TMessageRouter : IAzureEventHubsMessageRouter
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            return builder.Services.AddSingleton<AzureFunctionsInProcessMessageCorrelation>()
                          .AddEventHubsMessageRouting(implementationFactory, configureOptions);
        }
    }
}
