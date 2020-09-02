using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    internal class MessageHandlerRegistration<TMessage, TMessageContext> : IMessageHandler<TMessage, TMessageContext> 
        where TMessageContext : MessageContext
    {
        private readonly Func<TMessageContext, bool> _messageContextFilter;
        private readonly IMessageHandler<TMessage, TMessageContext> _messageHandlerImplementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerRegistration"/> class.
        /// </summary>
        internal MessageHandlerRegistration(
            Func<TMessageContext, bool> messageContextFilter,
            IMessageHandler<TMessage, TMessageContext> messageHandlerImplementation)
        {
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter));
            Guard.NotNull(messageHandlerImplementation, nameof(messageHandlerImplementation));

            _messageContextFilter = messageContextFilter;
            _messageHandlerImplementation = messageHandlerImplementation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageContext"></param>
        /// <returns></returns>
        internal bool CanProcessMessage(TMessageContext messageContext)
        {
            return _messageContextFilter(messageContext);
        }

        /// <summary>
        /// Process the given <paramref name="message"/> in the current <see cref="IMessageHandler{TMessage,TMessageContext}"/> representation.
        /// </summary>
        /// <typeparam name="TMessageContext">The type of the message context used in the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</typeparam>
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
            await _messageHandlerImplementation.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
        }
    }
}
