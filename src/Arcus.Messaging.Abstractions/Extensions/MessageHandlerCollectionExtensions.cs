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
        /// Adds an <see cref="IFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="collection">The collection to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message handling will be supported from now on")]
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
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message handling will be supported from now on")]
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
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message handling will be supported from now on")]
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
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message handling will be supported from now on")]
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
