using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an general <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class MessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="messageContextFilter">The function that determines if the message handler should handle the message based on the context.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/>, <paramref name="messageContextFilter"/>, or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message handling will be supported from now on")]
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection services,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
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

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            services.AddMessageHandler(implementationFactory, messageContextFilter: messageContextFilter);
            return services;
        }
    }
}
