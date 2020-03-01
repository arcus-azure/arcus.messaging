using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an general <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IMessageHandler{TMessage, TMessageContext}" /> implementation to process the messages from an <see cref="MessagePump"/> implementation.
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message handler will process the message.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        public static IServiceCollection WithMessageHandler<TMessageHandler, TMessage, TMessageContext>(this IServiceCollection services)
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext> 
            where TMessage : class
            where TMessageContext : MessageContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddSingleton<IMessageHandler<TMessage, TMessageContext>, TMessageHandler>();

            return services;
        }
    }
}
