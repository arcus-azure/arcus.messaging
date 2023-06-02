using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using GuardNet;

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
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageContextFilter, messageBodySerializer);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer to deserialize the incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageContextFilter, messageBodySerializerImplementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the service.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent services");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                messageContextFilter, messageBodySerializer, messageHandlerImplementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the service.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<MessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer to deserialize the incoming message for the message handler");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent services");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                messageContextFilter, 
                messageBodySerializerImplementationFactory, 
                messageHandlerImplementationFactory);
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
        /// <param name="messageBodySerializer">The function that creates the <see cref="IMessageBodySerializer"/> implementation.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageContextFilter, messageBodySerializer, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
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
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <param name="messageBodySerializer">The <see cref="IMessageBodySerializer"/> that custom deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent services");

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageContextFilter: messageContextFilter, messageBodySerializerImplementationFactory: _ => messageBodySerializer, messageHandlerImplementationFactory);
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
        /// <param name="messageBodySerializerImplementationFactory">The function that creates the <see cref="IMessageBodySerializer"/> implementation.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer to deserialize the incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageContextFilter, messageBodySerializerImplementationFactory, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
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
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function that creates the <see cref="IMessageBodySerializer"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageContextFilter,  nameof(messageContextFilter), "Requires a filter to restrict the message processing within a certain message context");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent services");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer to deserialize the incoming message for the message handler");

            services.AddMessageHandler(
                messageHandlerImplementationFactory, 
                messageContextFilter: messageContextFilter, 
                implementationFactoryMessageBodySerializer: messageBodySerializerImplementationFactory);
            
            return services;
        }
    }
}
