using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.AzureFunctions.EventHubs;
using GuardNet;
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
            Guard.NotNull(builder, nameof(builder), "Requires a functions host builder to add the Azure EventHubs message router");

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
            Guard.NotNull(builder, nameof(builder), "Requires a functions host builder to add the Azure EventHubs message router");

            return builder.Services.AddSingleton<AzureFunctionsInProcessMessageCorrelation>()
                          .AddEventHubsMessageRouting(configureOptions);
        }
    }
}
