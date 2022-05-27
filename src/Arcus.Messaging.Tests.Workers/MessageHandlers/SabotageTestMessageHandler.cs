using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    /// <summary>
    /// Represents a <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation that sabotages the message processing by throwing an exception.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to handle.</typeparam>
    /// <typeparam name="TMessageContext">The type of the context to handle.</typeparam>
    public class SabotageTestMessageHandler<TMessage, TMessageContext> : IMessageHandler<TMessage, TMessageContext> 
        where TMessageContext : MessageContext
    {
        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        public Task ProcessMessageAsync(
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            throw new AccessViolationException("Sabotage message processing!");
        }
    }
}
