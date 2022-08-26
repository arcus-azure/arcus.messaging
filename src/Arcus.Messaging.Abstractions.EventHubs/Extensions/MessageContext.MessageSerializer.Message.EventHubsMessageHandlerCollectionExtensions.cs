using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureEventHubsMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class EventHubsMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IAzureEventHubsMessageHandler{TMessage}" /> implementation to process the messages from an Azure EventHubs.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializer"/>, or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection WithEventHubsMessageHandler<TMessageHandler, TMessage>(
            this EventHubsMessageHandlerCollection handlers,
            Func<AzureEventHubsMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IAzureEventHubsMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureEventHubsMessageContext>(messageContextFilter, messageBodySerializer, messageBodyFilter);
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureEventHubsMessageHandler{TMessage}" /> implementation to process the messages from an Azure EventHubs.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializerImplementationFactory"/>, or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection WithEventHubsMessageHandler<TMessageHandler, TMessage>(
            this EventHubsMessageHandlerCollection handlers,
            Func<AzureEventHubsMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IAzureEventHubsMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureEventHubsMessageContext>(messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter);
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureEventHubsMessageHandler{TMessage}" /> implementation to process the messages from an Azure EventHubs.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializer"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection WithEventHubsMessageHandler<TMessageHandler, TMessage>(
            this EventHubsMessageHandlerCollection handlers,
            Func<AzureEventHubsMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureEventHubsMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureEventHubsMessageContext>(messageContextFilter, messageBodySerializer, messageBodyFilter, implementationFactory);
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureEventHubsMessageHandler{TMessage}" /> implementation to process the messages from an Azure EventHubs.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageHandlerImplementationFactory">The that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializerImplementationFactory"/>, <paramref name="messageBodyFilter"/>, or <paramref name="messageHandlerImplementationFactory"/> is <c>null</c>.</exception>
        public static EventHubsMessageHandlerCollection WithEventHubsMessageHandler<TMessageHandler, TMessage>(
            this EventHubsMessageHandlerCollection handlers,
            Func<AzureEventHubsMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IAzureEventHubsMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(handlers, nameof(handlers), "Requires a set of handlers to add the message handler");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler");

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureEventHubsMessageContext>(messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter, messageHandlerImplementationFactory);
            return handlers;
        }
    }
}
