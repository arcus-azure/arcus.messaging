using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        private readonly Func<string, bool> _messageBodyFilter;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerRegistration{TMessage, TMessageContext}"/> class.
        /// </summary>
        /// <param name="messageContextFilter">The filter to determine if a given <see cref="MessageContext"/> can be handled by the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.</param>
        /// <param name="messageHandlerImplementation">The <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation that this registration instance represents.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the restriction filters.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandlerImplementation"/> is <c>null</c>.</exception>
        internal MessageHandlerRegistration(
            Func<TMessageContext, bool> messageContextFilter,
            IMessageHandler<TMessage, TMessageContext> messageHandlerImplementation,
            ILogger<MessageHandlerRegistration<TMessage, TMessageContext>> logger)
        {
            Guard.NotNull(messageHandlerImplementation, nameof(messageHandlerImplementation), "Requires a message handler implementation to apply the message processing filters to");
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter), "Requires a message context filter to restrict message processing within a certain message context");

            _messageContextFilter = messageContextFilter;
            _logger = logger ?? NullLogger<MessageHandlerRegistration<TMessage, TMessageContext>>.Instance;
            
            Service = messageHandlerImplementation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerRegistration{TMessage, TMessageContext}"/> class.
        /// </summary>
        /// <param name="messageBodyFilter">The filter to determine if a given message body can be handled by the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.</param>
        /// <param name="messageHandlerImplementation">The <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation that this registration instance represents.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the restriction filters.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandlerImplementation"/> is <c>null</c>.</exception>
        internal MessageHandlerRegistration(
            Func<string, bool> messageBodyFilter,
            IMessageHandler<TMessage, TMessageContext> messageHandlerImplementation,
            ILogger<MessageHandlerRegistration<TMessage, TMessageContext>> logger)
        {
            Guard.NotNull(messageHandlerImplementation, nameof(messageHandlerImplementation), "Requires a message handler implementation to apply the message processing filters to");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a message body filter to restrict the message processing based on the message body");

            _messageBodyFilter = messageBodyFilter;
            _logger = logger ?? NullLogger<MessageHandlerRegistration<TMessage, TMessageContext>>.Instance;
            
            Service = messageHandlerImplementation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerRegistration{TMessage, TMessageContext}"/> class.
        /// </summary>
        /// <param name="messageContextFilter">The filter to determine if a given <see cref="MessageContext"/> can be handled by the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.</param>
        /// <param name="messageBodyFilter">The filter to determine if a given message body can be handled by the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.</param>
        /// <param name="messageHandlerImplementation">The <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation that this registration instance represents.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the restriction filters.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandlerImplementation"/> is <c>null</c>.</exception>
        internal MessageHandlerRegistration(
            Func<TMessageContext, bool> messageContextFilter,
            Func<string, bool> messageBodyFilter,
            IMessageHandler<TMessage, TMessageContext> messageHandlerImplementation,
            ILogger<MessageHandlerRegistration<TMessage, TMessageContext>> logger)
        {
            Guard.NotNull(messageHandlerImplementation, nameof(messageHandlerImplementation), "Requires a message handler implementation to apply the message processing filters to");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a message body filter to restrict the message processing based on the message body");
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter), "Requires a message context filter to restrict message processing within a certain message context");

            _messageContextFilter = messageContextFilter;
            _messageBodyFilter = messageBodyFilter;
            _logger = logger ?? NullLogger<MessageHandlerRegistration<TMessage, TMessageContext>>.Instance;
            
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
        /// Determine if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the messages based on the raw message body.
        /// </summary>
        /// <param name="messageBody">The raw message body that's about to be deserialized.</param>
        /// <returns>
        ///     [true] if the <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the message body; [false] otherwise.
        /// </returns>
        internal bool CanProcessMessageBasedOnMessageBody(string messageBody)
        {
            try
            {
                return _messageBodyFilter?.Invoke(messageBody) ?? true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to run message body predicate for {MessageHandlerType}", Service.GetType().Name);
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
