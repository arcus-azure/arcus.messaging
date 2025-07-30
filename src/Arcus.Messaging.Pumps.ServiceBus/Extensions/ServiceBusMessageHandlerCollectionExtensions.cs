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
        /// <exception cref="NotSupportedException">Thrown when the message pump was configured to use sessions.</exception>
        public static ServiceBusMessageHandlerCollection WithCircuitBreakerStateChangedEventHandler<TEventHandler>(
            this ServiceBusMessageHandlerCollection collection)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            return WithCircuitBreakerStateChangedEventHandler(collection, provider => ActivatorUtilities.CreateInstance<TEventHandler>(provider));
        }

        /// <summary>
        /// Adds an <see cref="ICircuitBreakerEventHandler"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TEventHandler">The custom type of the event handler.</typeparam>
        /// <param name="collection">The application services to register the event handler.</param>
        /// <param name="implementationFactory">The factory function to create the custom <see cref="ICircuitBreakerEventHandler"/> implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the message pump was configured to use sessions.</exception>
        public static ServiceBusMessageHandlerCollection WithCircuitBreakerStateChangedEventHandler<TEventHandler>(
            this ServiceBusMessageHandlerCollection collection,
            Func<IServiceProvider, TEventHandler> implementationFactory)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(implementationFactory);

            if (collection.UseSessions)
            {
                throw new NotSupportedException(
                    "Cannot register a circuit breaker event handler for an Azure Service Bus message pump that uses sessions, " +
                    "because the circuit breaker functionality is not supported for session-based message pumps.");
            }

            collection.Services.AddTransient(serviceProvider => new CircuitBreakerEventHandler(collection.JobId, implementationFactory(serviceProvider)));
            return collection;
        }
    }

    /// <summary>
    /// Represents a registration of an <see cref="ICircuitBreakerEventHandler"/> instance in the application services,
    /// specifically linked to a message pump.
    /// </summary>
    internal sealed class CircuitBreakerEventHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerEventHandler" /> class.
        /// </summary>
        public CircuitBreakerEventHandler(string jobId, ICircuitBreakerEventHandler handler)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
            ArgumentNullException.ThrowIfNull(handler);

            JobId = jobId;
            Handler = handler;
        }

        /// <summary>
        /// Gets the unique ID to distinguish the linked message pump.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Gets the event handler implementation to trigger on transition changes in the linked message pump.
        /// </summary>
        public ICircuitBreakerEventHandler Handler { get; }
    }
}
