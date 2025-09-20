using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class ServiceBusMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(this ServiceBusMessageHandlerCollection handlers)
            where TMessageHandler : class, IServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            return WithServiceBusMessageHandler<TMessageHandler, TMessage>(handlers, configureOptions: null);
        }

        /// <summary>
        /// Adds a <see cref="IServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="configureOptions">The additional set of options to configure the registration of the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Action<ServiceBusMessageHandlerOptions<TMessage>> configureOptions)
            where TMessageHandler : class, IServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            return WithServiceBusMessageHandler(handlers, provider => ActivatorUtilities.CreateInstance<TMessageHandler>(provider), configureOptions);
        }

        /// <summary>
        /// Adds a <see cref="IServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            return WithServiceBusMessageHandler<TMessageHandler, TMessage>(handlers, implementationFactory, configureOptions: null);
        }

        /// <summary>
        /// Adds a <see cref="IServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
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
            where TMessageHandler : class, IServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(handlers);
            ArgumentNullException.ThrowIfNull(implementationFactory);

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
    }

    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class DeprecatedServiceBusMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="handlers">The collection of handlers to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v4.0, please implement " + nameof(IServiceBusMessageHandler<object>) + " instead")]
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
        [Obsolete("Will be removed in v4.0, please implement " + nameof(IServiceBusMessageHandler<object>) + " instead")]
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
        [Obsolete("Will be removed in v4.0, please implement " + nameof(IServiceBusMessageHandler<object>) + " instead")]
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
        [Obsolete("Will be removed in v4.0, please implement " + nameof(IServiceBusMessageHandler<object>) + " instead")]
        public static ServiceBusMessageHandlerCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection handlers,
            Func<IServiceProvider, TMessageHandler> implementationFactory,
            Action<ServiceBusMessageHandlerOptions<TMessage>> configureOptions)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(handlers);
            ArgumentNullException.ThrowIfNull(implementationFactory);

            return handlers.WithServiceBusMessageHandler(
                serviceProvider => new DeprecatedServiceBusMessageHandlerAdapter<TMessage>(implementationFactory(serviceProvider)),
                configureOptions);
        }

        [Obsolete("Will be removed in v4.0")]
        private class DeprecatedServiceBusMessageHandlerAdapter<TMessage> : IServiceBusMessageHandler<TMessage>
        {
            private readonly IAzureServiceBusMessageHandler<TMessage> _deprecatedHandler;

            internal DeprecatedServiceBusMessageHandlerAdapter(IAzureServiceBusMessageHandler<TMessage> deprecatedHandler)
            {
                _deprecatedHandler = deprecatedHandler;
            }

            public Task ProcessMessageAsync(
                TMessage message,
                ServiceBusMessageContext messageContext,
                MessageCorrelationInfo correlationInfo,
                CancellationToken cancellationToken)
            {
                var deprecatedContext = new AzureServiceBusMessageContext(messageContext);
                return _deprecatedHandler.ProcessMessageAsync(message, deprecatedContext, correlationInfo, cancellationToken);
            }
        }
    }
}
