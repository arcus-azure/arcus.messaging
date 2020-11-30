using System;
using System.Collections.Generic;
using System.Text;

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
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<MessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                messageContextFilter, messageBodySerializer, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageBodySerializer">The type of the <see cref="IMessageBodySerializer"/> implementation.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageBodySerializer>(
            this IServiceCollection services,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, TMessageBodySerializer> messageBodySerializerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
            where TMessageBodySerializer : IMessageBodySerializer
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageBodySerializer>(
                messageContextFilter, messageBodySerializerImplementationFactory, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageBodySerializer">The type of the custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the service.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageBodySerializer>(
            this IServiceCollection services,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, TMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
            where TMessageBodySerializer : IMessageBodySerializer
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory));
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory));

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageBodySerializer>(
                messageContextFilter, 
                messageBodySerializerImplementationFactory, 
                messageHandlerImplementationFactory);
        }

        /// <summary>
         /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
         /// resources.
         /// </summary>
         /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
         /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
         /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
         /// <param name="services">The collection of services to use in the application.</param>
         /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
         /// <param name="messageBodySerializer">The function that creates the <see cref="IMessageBodySerializer"/> implementation.</param>
         public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
             this IServiceCollection services,
             Func<TMessageContext, bool> messageContextFilter,
             IMessageBodySerializer messageBodySerializer)
             where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
             where TMessage : class
             where TMessageContext : MessageContext
         {
             Guard.NotNull(services, nameof(services));
             Guard.NotNull(messageContextFilter, nameof(messageContextFilter));
             Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

             return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                 messageContextFilter, messageBodySerializer, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
         }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <param name="messageBodySerializer">The <see cref="IMessageBodySerializer"/> that custom deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this IServiceCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter));
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory));
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.AddTransient<IMessageHandler<TMessage, TMessageContext>, MessageHandlerRegistration<TMessage, TMessageContext>>(
                serviceProvider => new MessageHandlerRegistration<TMessage, TMessageContext>(
                    messageContextFilter,
                    messageBodySerializer,
                    messageHandlerImplementationFactory(serviceProvider)));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <typeparam name="TMessageBodySerializer">The type of the <see cref="IMessageBodySerializer"/>.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function that creates the <see cref="IMessageBodySerializer"/> implementation.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext, TMessageBodySerializer>(
            this IServiceCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, TMessageBodySerializer> messageBodySerializerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
            where TMessageBodySerializer : IMessageBodySerializer
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory));

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext, TMessageBodySerializer>(
                messageContextFilter, messageBodySerializerImplementationFactory, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <typeparam name="TMessageBodySerializer">The type of the custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function that creates the <see cref="IMessageBodySerializer"/>.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext, TMessageBodySerializer>(
            this IServiceCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, TMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
            where TMessageBodySerializer : IMessageBodySerializer
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter));
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory));
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory));

            return services.AddTransient<IMessageHandler<TMessage, TMessageContext>, MessageHandlerRegistration<TMessage, TMessageContext>>(
                serviceProvider => new MessageHandlerRegistration<TMessage, TMessageContext>(
                    messageContextFilter,
                    messageBodySerializerImplementationFactory(serviceProvider),
                    messageHandlerImplementationFactory(serviceProvider)));
        }
    }
}
