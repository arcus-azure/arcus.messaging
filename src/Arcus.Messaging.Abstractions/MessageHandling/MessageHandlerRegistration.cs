using System;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Abstractions.MessageHandling
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
        private readonly Func<TMessage, bool> _messageBodyFilter;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerRegistration{TMessage, TMessageContext}"/> class.
        /// </summary>
        /// <param name="messageContextFilter">The filter to determine if a given <see cref="MessageContext"/> can be handled by the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.</param>
        /// <param name="messageBodySerializer">The optional custom serializer that will deserialize the incoming message for the <paramref name="messageHandlerImplementation"/>.</param>
        /// <param name="messageBodyFilter">The filter to determine if a given message can be handled by the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.</param>
        /// <param name="messageHandlerImplementation">The <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation that this registration instance represents.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the restriction filters.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandlerImplementation"/> is <c>null</c>.</exception>
        internal MessageHandlerRegistration(
            Func<TMessageContext, bool> messageContextFilter,
            IMessageBodySerializer messageBodySerializer,
            Func<TMessage, bool> messageBodyFilter,
            IMessageHandler<TMessage, TMessageContext> messageHandlerImplementation,
            ILogger<MessageHandlerRegistration<TMessage, TMessageContext>> logger)
        {
            Guard.NotNull(messageHandlerImplementation, nameof(messageHandlerImplementation), "Requires a message handler implementation to apply the message processing filters to");

            _messageContextFilter = messageContextFilter;
            _messageBodyFilter = messageBodyFilter;
            _logger = logger ?? NullLogger<MessageHandlerRegistration<TMessage, TMessageContext>>.Instance;

            Service = messageHandlerImplementation;
            Serializer = messageBodySerializer;
        }

        /// <summary>
        /// Gets the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
        /// </summary>
        internal IMessageHandler<TMessage, TMessageContext> Service { get; }

        /// <summary>
        /// Gets the optional <see cref="IMessageBodySerializer"/> implementation that will custom deserialize the incoming message for the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
        /// </summary>
        internal IMessageBodySerializer Serializer { get; }

        /// <summary>
        /// Determine if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process messages within the given <paramref name="messageContext"/> type.
        /// </summary>
        /// <param name="messageContext">The specific message context, providing information about the received message.</param>
        /// <returns>
        ///     [true] if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the message within the given <paramref name="messageContext"/>; [false] otherwise.
        /// </returns>
        internal bool CanProcessMessageWithinMessageContext(TMessageContext messageContext)
        {
            try
            {
                return _messageContextFilter?.Invoke(messageContext) ?? true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to run message context {MessageContextType} predicate for {MessageHandlerType}", typeof(TMessageContext).Name, Service.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Determine if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the messages based on the incoming deserialized message.
        /// </summary>
        /// <param name="message">The incoming message that's deserialized.</param>
        /// <returns>
        ///     [true] if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the message; [false] otherwise.
        /// </returns>
        internal bool CanProcessMessageBasedOnMessage(TMessage message)
        {
            try
            {
                return _messageBodyFilter?.Invoke(message) ?? true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to run message predicate for {MessageHandlerType}", Service.GetType().Name);
                return false;
            }
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
