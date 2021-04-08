using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddServiceBusMessageRouting(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure Service Bus message routing");

            return services.AddServiceBusMessageRouting(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<AzureServiceBusMessageRouter>>();
                return new AzureServiceBusMessageRouter(serviceProvider, logger);
            });
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureServiceBusMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IServiceCollection AddServiceBusMessageRouting<TMessageRouter>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageRouter> implementationFactory)
            where TMessageRouter : IAzureServiceBusMessageRouter
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure Service Bus message routing");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the Azure Service Bus message router");

            services.TryAddSingleton<IAzureServiceBusMessageRouter>(serviceProvider => implementationFactory(serviceProvider));
            services.AddMessageRouting(serviceProvider => serviceProvider.GetRequiredService<IAzureServiceBusMessageRouter>());

            return services;
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(this IServiceCollection services)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");

            return services.AddTransient<IMessageHandler<TMessage, AzureServiceBusMessageContext>, TMessageHandler>();
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent services");

            return services.AddTransient<IMessageHandler<TMessage, AzureServiceBusMessageContext>, TMessageHandler>(implementationFactory);
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusFallbackMessageHandler<TMessageHandler>(
            this IServiceCollection services)
            where TMessageHandler : class, IAzureServiceBusFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the Azure Service Bus fallback message handler to");

            return services.AddTransient<IAzureServiceBusFallbackMessageHandler, TMessageHandler>();
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <param name="createImplementation">The function to create the fallback message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusFallbackMessageHandler<TMessageHandler>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> createImplementation)
            where TMessageHandler : class, IAzureServiceBusFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the fallback message handler to");
            Guard.NotNull(createImplementation, nameof(createImplementation), "Requires a function to create the fallback message handler");

            return services.AddTransient<IAzureServiceBusFallbackMessageHandler, TMessageHandler>(createImplementation);
        }
    }
}
