using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an general <see cref="IMessageHandler{TMessage}"/> implementation.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection collection,
            Func<MessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            return collection.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageContextFilter, messageBodySerializer, messageBodyFilter);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection collection,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            return collection.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection collection,
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            return collection.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageContextFilter, messageBodySerializer, messageBodyFilter, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection collection,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            return collection.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The function that creates the service.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection collection,
            Func<MessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent collection");

            return collection.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                messageContextFilter, messageBodySerializer, messageBodyFilter, implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the service.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/>, <paramref name="messageBodyFilter"/>, or <paramref name="messageHandlerImplementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection collection,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent collection");

            return collection.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter, messageHandlerImplementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection collection,
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent collection");

            collection.Services.AddTransient<IMessageHandler<TMessage, TMessageContext>, MessageHandlerRegistration<TMessage, TMessageContext>>(
                serviceProvider => new MessageHandlerRegistration<TMessage, TMessageContext>(
                    messageContextFilter: messageContextFilter,
                    messageBodySerializer: messageBodySerializer,
                    messageBodyFilter: messageBodyFilter,
                    messageHandlerImplementation: implementationFactory(serviceProvider),
                    logger: serviceProvider.GetService<ILogger<MessageHandlerRegistration<TMessage, TMessageContext>>>()));

            return collection;
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/>, <paramref name="messageBodyFilter"/>, or <paramref name="messageHandlerImplementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection collection,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(collection, nameof(collection), "Requires a set of collection to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent collection");

            collection.Services.AddTransient<IMessageHandler<TMessage, TMessageContext>, MessageHandlerRegistration<TMessage, TMessageContext>>(
                serviceProvider => new MessageHandlerRegistration<TMessage, TMessageContext>(
                    messageContextFilter: messageContextFilter,
                    messageBodySerializer: messageBodySerializerImplementationFactory(serviceProvider),
                    messageBodyFilter: messageBodyFilter,
                    messageHandlerImplementation: messageHandlerImplementationFactory(serviceProvider),
                    logger: serviceProvider.GetService<ILogger<MessageHandlerRegistration<TMessage, TMessageContext>>>()));

            return collection;
        }
    }
}
