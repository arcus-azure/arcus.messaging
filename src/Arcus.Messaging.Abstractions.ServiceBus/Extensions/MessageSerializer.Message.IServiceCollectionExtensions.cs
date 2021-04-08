using System;
using Arcus.Messaging.Abstractions.MessageHandling;
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
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodySerializer"/>, or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            return services.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageBodySerializer, messageBodyFilter);
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializer">The custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodySerializer"/>, <paramref name="messageBodyFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<TMessage, bool> messageBodyFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializer, nameof(messageBodySerializer), "Requires an custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent services");

            return services.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageBodySerializer, messageBodyFilter, implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create the custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodySerializerImplementationFactory"/>, or <paramref name="messageBodyFilter"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<TMessage, bool> messageBodyFilter)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create the custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");

            return services.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageBodySerializerImplementationFactory, messageBodyFilter);
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageBodySerializerImplementationFactory">The function to create the custom <see cref="IMessageBodySerializer"/> to deserialize the incoming message for the <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.</param>
        /// <param name="messageBodyFilter">The filter to restrict the message processing based on the incoming message body.</param>
        /// <param name="messageHandlerImplementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageBodySerializerImplementationFactory"/>, <paramref name="messageBodyFilter"/>, or <paramref name="messageHandlerImplementationFactory"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<TMessage, bool> messageBodyFilter,
            Func<IServiceProvider, IMessageBodySerializer> messageBodySerializerImplementationFactory,
            Func<IServiceProvider, TMessageHandler> messageHandlerImplementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(messageBodySerializerImplementationFactory, nameof(messageBodySerializerImplementationFactory), "Requires a function to create the custom message body serializer instance to deserialize incoming message for the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a filter to restrict the message processing based on the incoming message body");
            Guard.NotNull(messageHandlerImplementationFactory, nameof(messageHandlerImplementationFactory), "Requires a function to create the message handler with dependent services");

            return services.WithMessageHandler<TMessageHandler, TMessage, AzureServiceBusMessageContext>(messageBodySerializerImplementationFactory, messageBodyFilter, messageHandlerImplementationFactory);
        }
    }
}
