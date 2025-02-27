﻿using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

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
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(this ServiceBusMessageHandlerCollection handlers)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            return WithServiceBusMessageHandler<TMessageHandler, TMessage>(handlers, configureOptions: null);
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="configureOptions">The additional set of options to configure the registration of the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Action<ServiceBusMessageHandlerOptions<TMessage>> configureOptions)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            return WithServiceBusMessageHandler(handlers, provider => ActivatorUtilities.CreateInstance<TMessageHandler>(provider), configureOptions);
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            return WithServiceBusMessageHandler<TMessageHandler, TMessage>(handlers, implementationFactory, configureOptions: null);
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <param name="configureOptions">The additional set of options to configure the registration of the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<IServiceProvider, TMessageHandler> implementationFactory,
            Action<ServiceBusMessageHandlerOptions<TMessage>> configureOptions)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            if (handlers is null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            var options = new ServiceBusMessageHandlerOptions<TMessage>();
            configureOptions?.Invoke(options);

            handlers.Services.AddTransient(
                serviceProvider => MessageHandler.Create(
                    implementationFactory(serviceProvider),
                    serviceProvider.GetService<ILogger<TMessageHandler>>(),
                    handlers.JobId,
                    options.MessageBodyFilter,
                    options.MessageContextFilter,
                    options.MessageBodySerializerImplementationFactory?.Invoke(serviceProvider)));

            return handlers;
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="handlers">The handlers to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusFallbackMessageHandler<TMessageHandler>(
            this ServiceBusMessageHandlerCollection handlers)
            where TMessageHandler : IAzureServiceBusFallbackMessageHandler
        {
            if (handlers is null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            handlers.WithServiceBusFallbackMessageHandler(serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
            return handlers;
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="handlers">The handlers to add the fallback message handler to.</param>
        /// <param name="createImplementation">The function to create the fallback message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusFallbackMessageHandler<TMessageHandler>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<IServiceProvider, TMessageHandler> createImplementation)
            where TMessageHandler : IAzureServiceBusFallbackMessageHandler
        {
            if (handlers is null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            handlers.AddFallbackMessageHandler<TMessageHandler, ServiceBusReceivedMessage, AzureServiceBusMessageContext>(createImplementation);
            return handlers;
        }
    }
}
