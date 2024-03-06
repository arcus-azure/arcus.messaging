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
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            IMessageBodySerializer messageBodySerializer)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageBodySerializer);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            IMessageBodySerializer messageBodySerializer)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageBodySerializer, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializer">The function to create the custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        /// <param name="implementationFactory">The function that creates the service.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            IMessageBodySerializer messageBodySerializer,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent services");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageBodySerializer, implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <param name="messageBodySerializer">The <see cref="IMessageBodySerializer"/> that custom deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            IMessageBodySerializer messageBodySerializer,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent services");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageBodySerializerImplementationFactory: _ => messageBodySerializer, 
                messageHandlerImplementationFactory: implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(messageBodySerializerImplementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                messageBodySerializerImplementationFactory, 
                serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create the custom <see cref="IMessageBodySerializer"/> that deserializes the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the service.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection services,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory));
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");

            return services.WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
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
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create an custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <typeparamref name="TMessageHandler"/>.</param>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent services");

            services.AddMessageHandler(messageHandlerImplementationFactory, implementationFactoryMessageBodySerializer: messageBodySerializerImplementationFactory);
            return services;
        }
    }
}
