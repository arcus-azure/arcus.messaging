﻿using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class ServiceBusMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializer"/>, or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(WithServiceBusMessageHandler) + " overload with options to add additional information to the message handler registration")]
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<AzureServiceBusMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            if (handlers is null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageContextFilter, messageBodySerializer, messageBodyFilter);
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create a custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializerImplementationFactory"/>, or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(WithServiceBusMessageHandler) + " overload with options to add additional information to the message handler registration")]
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<AzureServiceBusMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            if (handlers is null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            handlers.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter);
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializer"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(WithServiceBusMessageHandler) + " overload with options to add additional information to the message handler registration")]
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<AzureServiceBusMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            if (handlers is null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            handlers.WithMessageHandler(messageContextFilter, messageBodySerializer, messageBodyFilter, implementationFactory);
            return handlers;
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create a custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageHandlerImplementationFactory">The that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/>, <paramref name="messageContextFilter"/>, <paramref name="messageBodySerializerImplementationFactory"/>, <paramref name="messageBodyFilter"/>, or <paramref name="messageHandlerImplementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(WithServiceBusMessageHandler) + " overload with options to add additional information to the message handler registration")]
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<AzureServiceBusMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            if (handlers is null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            handlers.WithMessageHandler(messageContextFilter, messageBodySerializerImplementationFactory, messageBodyFilter, messageHandlerImplementationFactory);
            return handlers;
        }
    }
}
