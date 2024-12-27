using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class ServiceBusMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="ICircuitBreakerEventHandler"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TEventHandler">The custom type of the event handler.</typeparam>
        /// <param name="collection">The application services to register the event handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithCircuitBreakerEventHandler<TEventHandler>(
            this ServiceBusMessageHandlerCollection collection)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            return WithCircuitBreakerEventHandler(collection, provider => ActivatorUtilities.CreateInstance<TEventHandler>(provider));
        }

        /// <summary>
        /// Adds an <see cref="ICircuitBreakerEventHandler"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TEventHandler">The custom type of the event handler.</typeparam>
        /// <param name="collection">The application services to register the event handler.</param>
        /// <param name="implementationFactory">The factory function to create the custom <see cref="ICircuitBreakerEventHandler"/> implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithCircuitBreakerEventHandler<TEventHandler>(
            this ServiceBusMessageHandlerCollection collection,
            Func<IServiceProvider, TEventHandler> implementationFactory)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            collection.Services.AddCircuitBreakerEventHandler(collection.JobId, implementationFactory);
            return collection;
        }
    }
}
