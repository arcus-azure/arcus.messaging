using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an general <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="MessageRouter"/> implementation to route the incoming messages through registered <see cref="IMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection AddMessageRouting(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message routing");

            MessageHandlerCollection collection = AddMessageRouting(services, configureOptions: null);
            return collection;
        }

        /// <summary>
        /// Adds a <see cref="MessageRouter"/> implementation to route the incoming messages through registered <see cref="IMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="configureOptions">The consumer-configurable options to change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection AddMessageRouting(this IServiceCollection services, Action<MessageRouterOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message routing");
            
            MessageHandlerCollection collection = AddMessageRouting(services, serviceProvider =>
            {
                var options = new MessageRouterOptions();
                configureOptions?.Invoke(options);
                var logger = serviceProvider.GetService<ILogger<MessageRouter>>();

                return new MessageRouter(serviceProvider, options, logger);
            });

            return collection;
        }

        /// <summary>
        /// Adds a <see cref="MessageRouter"/> implementation to route the incoming messages through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.
        /// </summary>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IMessageRouter"/> implementation.</typeparam>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <see cref="MessageRouter"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection AddMessageRouting<TMessageRouter>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageRouter> implementationFactory)
            where TMessageRouter : IMessageRouter
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message routing");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message router");

            services.AddSingleton<IMessageRouter>(serviceProvider => implementationFactory(serviceProvider));
            return new MessageHandlerCollection(services);
        }
    }
}
