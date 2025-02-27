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
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(this MessageHandlerCollection collection)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            return WithMessageHandler<TMessageHandler, TMessage, MessageContext>(
                collection, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage}" /> implementation to process the messages.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the service.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage>(
            this MessageHandlerCollection collection,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, MessageContext>
            where TMessage : class
        {
            return WithMessageHandler<TMessageHandler, TMessage, MessageContext>(collection, implementationFactory);
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(this MessageHandlerCollection collection)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext> 
            where TMessage : class
            where TMessageContext : MessageContext
        {
            return WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
                collection, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an Azure Service Bus.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="collection">The collection of collection to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            this MessageHandlerCollection collection,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            collection.AddMessageHandler(implementationFactory);
            return collection;
        }

        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="collection">The collection to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithFallbackMessageHandler<TMessageHandler>(this MessageHandlerCollection collection)
            where TMessageHandler : IFallbackMessageHandler<string, MessageContext>
        {
            return WithFallbackMessageHandler(collection, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        /// <param name="collection">The collection to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithFallbackMessageHandler<TMessageHandler, TMessageContext>(this MessageHandlerCollection collection)
            where TMessageHandler : IFallbackMessageHandler<string, TMessageContext>
            where TMessageContext : MessageContext
        {
            return WithFallbackMessageHandler<TMessageHandler, TMessageContext>(collection, serviceProvider => ActivatorUtilities.CreateInstance<TMessageHandler>(serviceProvider));
        }

        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="collection">The collection to add the fallback message handler to.</param>
        /// <param name="createImplementation">The function to create the fallback message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithFallbackMessageHandler<TMessageHandler>(
            this MessageHandlerCollection collection,
            Func<IServiceProvider, TMessageHandler> createImplementation)
            where TMessageHandler : IFallbackMessageHandler<string, MessageContext>
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (createImplementation is null)
            {
                throw new ArgumentNullException(nameof(createImplementation));
            }

            collection.AddFallbackMessageHandler<TMessageHandler, string, MessageContext>(createImplementation);
            return collection;
        }

        /// <summary>
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        /// <param name="collection">The collection to add the fallback message handler to.</param>
        /// <param name="createImplementation">The function to create the fallback message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static MessageHandlerCollection WithFallbackMessageHandler<TMessageHandler, TMessageContext>(
            this MessageHandlerCollection collection,
            Func<IServiceProvider, TMessageHandler> createImplementation)
            where TMessageHandler : IFallbackMessageHandler<string, TMessageContext> 
            where TMessageContext : MessageContext
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (createImplementation is null)
            {
                throw new ArgumentNullException(nameof(createImplementation));
            }

            collection.AddFallbackMessageHandler<TMessageHandler, string, TMessageContext>(createImplementation);
            return collection;
        }
    }
}
