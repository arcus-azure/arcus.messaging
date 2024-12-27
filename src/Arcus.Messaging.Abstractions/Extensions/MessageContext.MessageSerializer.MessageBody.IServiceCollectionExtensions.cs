using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an general <see cref="IMessageHandler{TMessage}"/> implementation.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class MessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            return WithMessageHandler<TMessageHandler, TMessage, MessageContext>(services, messageContextFilter, messageBodySerializer, messageBodyFilter);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            return WithMessageHandler<TMessageHandler, TMessage, MessageContext>(services, messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            return WithMessageHandler(
                services, messageContextFilter, messageBodySerializer, messageBodyFilter, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            return WithMessageHandler(
                services, messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The function that creates the service.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            return WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                services, messageContextFilter, messageBodySerializer, messageBodyFilter, implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the service.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodyFilter"/>, or <paramref name="messageHandlerImplementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            return WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                services, messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter, messageHandlerImplementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            return WithMessageHandler(services, messageContextFilter, _ => messageBodySerializer, messageBodyFilter, implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodyFilter"/>, or <paramref name="messageHandlerImplementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (messageContextFilter is null)
            {
                throw new ArgumentNullException(nameof(messageContextFilter));
            }

            if (messageBodySerializerImplementationFactory is null)
            {
                throw new ArgumentNullException(nameof(messageBodySerializerImplementationFactory));
            }

            if (messageBodyFilter is null)
            {
                throw new ArgumentNullException(nameof(messageBodyFilter));
            }

            if (messageHandlerImplementationFactory is null)
            {
                throw new ArgumentNullException(nameof(messageHandlerImplementationFactory));
            }

            services.AddMessageHandler(messageHandlerImplementationFactory, messageBodyFilter, messageContextFilter, messageBodySerializerImplementationFactory);
            return services;
        }
    }
}
