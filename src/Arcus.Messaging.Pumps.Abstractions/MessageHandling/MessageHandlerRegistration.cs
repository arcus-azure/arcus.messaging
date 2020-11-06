using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents an specific <see cref="IMessageHandler{TMessage,TMessageContext}"/> registration that requires more metadata information than only the instance itself.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can handle.</typeparam>
    /// <typeparam name="TMessageContext">The type of the message context the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can handle.</typeparam>
    internal class MessageHandlerRegistration<TMessage, TMessageContext> : IMessageHandler<TMessage, TMessageContext> 
        where TMessageContext : MessageContext
    {
        private readonly Func<TMessageContext, bool> _messageContextFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerRegistration{TMessage, TMessageContext}"/> class.
        /// </summary>
        /// <param name="messageContextFilter">The filter to determine if a given <see cref="MessageContext"/> can be handled by the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.</param>
        /// <param name="messageHandlerImplementation">The <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation that this registration instance represents.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageContextFilter"/> or <paramref name="messageHandlerImplementation"/> is <c>null</c>.</exception>
        internal MessageHandlerRegistration(
            Func<TMessageContext, bool> messageContextFilter,
            IMessageHandler<TMessage, TMessageContext> messageHandlerImplementation)
        {
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));
            Guard.NotNull(messageHandlerImplementation, nameof(messageHandlerImplementation));

            _messageContextFilter = messageContextFilter;
            
            Service = messageHandlerImplementation;
        }

        /// <summary>
        /// Gets the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
        /// </summary>
        internal IMessageHandler<TMessage, TMessageContext> Service { get; }

        /// <summary>
        /// Determine if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process messages within the given <paramref name="messageContext"/> type.
        /// </summary>
        /// <param name="messageContext">The specific message context, providing information about the received message.</param>
        /// <returns>
        ///     [true] if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the message within the given <paramref name="messageContext"/>; [false] otherwise.
        /// </returns>
        internal bool CanProcessMessage(TMessageContext messageContext)
        {
            return _messageContextFilter(messageContext);
        }

        /// <summary>
        /// Process the given <paramref name="message"/> in the current <see cref="IMessageHandler{TMessage,TMessageContext}"/> representation.
        /// </summary>
        /// <param name="message">The parsed message to be processed by the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">
        ///     The information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task ProcessMessageAsync(
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await Service.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
        }
    }
}
