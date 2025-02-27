using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the model that exposes the available <see cref="IMessageHandler{TMessage}"/>s, <see cref="IFallbackMessageHandler"/>s,
    /// and possible additional configurations that can be configured with the current state.
    /// </summary>
    public class MessageHandlerCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerCollection" /> class.
        /// </summary>
        /// <param name="services">The current available collection services to register the message handling logic into.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public MessageHandlerCollection(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Gets or sets the unique ID to identify the job that runs a message pump during the lifetime of the application.
        /// This ID can be used to get a reference of the previously registered message pump while registering message handlers and other functionality related to the message pump.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Gets the current available collection of services to register the message handling logic into.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Adds a general <see cref="MessageHandler"/> instance to the registered application services.
        /// </summary>
        /// <typeparam name="TMessage">The type of message the message handler created from the <paramref name="implementationFactory"/> processes.</typeparam>
        /// <typeparam name="TMessageContext">The type of context the message handler created from the <paramref name="implementationFactory"/> processes.</typeparam>
        /// <param name="implementationFactory">The function to create an user-defined message handler instance.</param>
        /// <param name="messageBodyFilter">The optional function to filter on the message body before processing.</param>
        /// <param name="messageContextFilter">The optional function to filter on the message context before processing.</param>
        /// <param name="implementationFactoryMessageBodySerializer">The function to create an optional message body serializer instance to customize how the message should be deserialized.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        internal void AddMessageHandler<TMessage, TMessageContext>(
            Func<IServiceProvider, IMessageHandler<TMessage, TMessageContext>> implementationFactory,
            Func<TMessage, bool> messageBodyFilter,
            Func<TMessageContext, bool> messageContextFilter,
            Func<IServiceProvider, IMessageBodySerializer> implementationFactoryMessageBodySerializer)
            where TMessageContext : MessageContext
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            Services.AddTransient(
                serviceProvider => MessageHandler.Create(
                    implementationFactory(serviceProvider),
                    serviceProvider.GetService<ILogger<IMessageHandler<TMessage, TMessageContext>>>(),
                    JobId,
                    messageBodyFilter,
                    messageContextFilter,
                    implementationFactoryMessageBodySerializer?.Invoke(serviceProvider)));
        }

        /// <summary>
        /// Adds a general <see cref="FallbackMessageHandler{TMessage,TMessageContext}"/> instance to the registered application services.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the user-defined fallback message handler.</typeparam>
        /// <typeparam name="TMessage">The type of the message that this fallback message handler can handle.</typeparam>
        /// <typeparam name="TMessageContext">The type of the context in which the message is being processed.</typeparam>
        /// <param name="implementationFactory">The function to create the user-defined fallback message handler instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public void AddFallbackMessageHandler<TMessageHandler, TMessage, TMessageContext>(
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : IFallbackMessageHandler<TMessage, TMessageContext>
            where TMessage : class
            where TMessageContext : MessageContext
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            Services.AddSingleton(
                serviceProvider => FallbackMessageHandler<TMessage, TMessageContext>.Create(
                    implementationFactory(serviceProvider),
                    JobId,
                    serviceProvider.GetService<ILogger<IFallbackMessageHandler<TMessage, TMessageContext>>>()));
        }
    }
}
