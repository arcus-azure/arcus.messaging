using System;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureEventHubsMessageRouter"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class EventHubsMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IAzureEventHubsMessageHandler{TMessage}" /> implementation to process the messages from Azure Event Hubs
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection WithEventHubsMessageHandler<TMessageHandler, TMessage>(this EventHubsMessageHandlerCollection handlers)
            where TMessageHandler : class, IAzureEventHubsMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureEventHubsMessageContext>();
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureEventHubsMessageHandler{TMessage}" /> implementation to process the messages from Azure Event Hubs
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection WithEventHubsMessageHandler<TMessageHandler, TMessage>(
            this EventHubsMessageHandlerCollection handlers,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureEventHubsMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent handlers");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureEventHubsMessageContext>(implementationFactory);
            return handlers;
        }
    }
}
