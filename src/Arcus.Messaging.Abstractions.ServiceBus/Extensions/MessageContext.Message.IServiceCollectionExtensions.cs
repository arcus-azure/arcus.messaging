using System;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using GuardNet;

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
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<AzureServiceBusMessageContext, bool> messageContextFilter,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageContextFilter, messageBodyFilter);
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<AzureServiceBusMessageContext, bool> messageContextFilter,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent handlers");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageContextFilter, messageBodyFilter, implementationFactory);
            return handlers;
        }
    }
}
