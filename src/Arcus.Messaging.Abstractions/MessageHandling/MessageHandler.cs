using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessMessageAsync = System.Func<object, Arcus.Messaging.Abstractions.MessageContext, Arcus.Messaging.Abstractions.MessageCorrelationInfo, System.Threading.CancellationToken, System.Threading.Tasks.Task<Arcus.Messaging.Abstractions.MessageHandling.MessageProcessingResult>>;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents an abstracted form of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation to handle with different type of generic message and message context types.
    /// </summary>
    public class MessageHandler
    {
        private readonly object _messageHandlerInstance;
        private readonly ProcessMessageAsync _messageHandlerImplementation;
        private readonly IMessageBodySerializer _messageBodySerializer;
        private readonly Func<MessageContext, MessageHandlerSummary, bool> _messageContextFilter;
        private readonly Func<object, MessageHandlerSummary, bool> _messageBodyFilter;
        private readonly ILogger _logger;

        private MessageHandler(
            object messageHandlerInstance,
            ProcessMessageAsync messageHandlerImplementation,
            Type messageType,
            Type messageContextType,
            Func<MessageContext, MessageHandlerSummary, bool> messageContextFilter,
            Func<object, MessageHandlerSummary, bool> messageBodyFilter,
            IMessageBodySerializer messageBodySerializer,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(messageHandlerInstance);
            ArgumentNullException.ThrowIfNull(messageHandlerImplementation);
            ArgumentNullException.ThrowIfNull(messageContextFilter);
            ArgumentNullException.ThrowIfNull(messageBodyFilter);
            ArgumentNullException.ThrowIfNull(messageType);
            ArgumentNullException.ThrowIfNull(messageContextType);

            _messageHandlerInstance = messageHandlerInstance;
            _messageHandlerImplementation = messageHandlerImplementation;
            _messageContextFilter = messageContextFilter;
            _messageBodyFilter = messageBodyFilter;
            _messageBodySerializer = messageBodySerializer;
            _logger = logger ?? NullLogger.Instance;

            MessageType = messageType;
            MessageContextType = messageContextType;
            MessageHandlerType = messageHandlerInstance.GetType();
        }

        /// <summary>
        /// Gets the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        internal Type MessageHandlerType { get; }

        /// <summary>
        /// Gets the type of the message that this abstracted message handler can process.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the type of the message context that this abstracted message handler can process.
        /// </summary>
        public Type MessageContextType { get; }

        /// <summary>
        /// Subtract all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementations from the given <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="serviceProvider">The provided registered services collection.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the lifetime of the message handlers.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public static IEnumerable<MessageHandler> SubtractFrom(IServiceProvider serviceProvider, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            MessageHandler[] registrations =
                serviceProvider.GetServices<MessageHandler>()
                               .ToArray();

            return registrations;
        }

        /// <summary>
        /// Creates a general <see cref="MessageHandler"/> instance from the <paramref name="messageHandler"/> instance.
        /// </summary>
        /// <typeparam name="TMessage">The type of message the <paramref name="messageHandler"/> processes.</typeparam>
        /// <typeparam name="TMessageContext">The type of context the <paramref name="messageHandler"/> processes.</typeparam>
        /// <param name="messageHandler">The user-defined message handler instance.</param>
        /// <param name="jobId">The job ID to link this message handler to a registered message pump.</param>
        /// <param name="messageBodyFilter">The optional function to filter on the message body before processing.</param>
        /// <param name="messageContextFilter">The optional function to filter on the message context before processing.</param>
        /// <param name="messageBodySerializer">The optional message body serializer instance to customize how the message should be deserialized.</param>
        /// <param name="logger">The logger instance to write diagnostic messages during the message handler interaction.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandler"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public static MessageHandler Create<TMessage, TMessageContext>(
            IMessageHandler<TMessage, TMessageContext> messageHandler,
            ILogger logger,
            string jobId,
            Func<TMessage, bool> messageBodyFilter = null,
            Func<TMessageContext, bool> messageContextFilter = null,
            IMessageBodySerializer messageBodySerializer = null)
            where TMessageContext : MessageContext
        {
            ArgumentNullException.ThrowIfNull(messageHandler);

            ProcessMessageAsync processMessageAsync = DetermineMessageImplementation(messageHandler);
            logger ??= NullLogger.Instance;

            return new MessageHandler(
                messageHandlerInstance: messageHandler,
                messageHandlerImplementation: processMessageAsync,
                messageType: typeof(TMessage),
                messageContextType: typeof(TMessageContext),
                messageContextFilter: DetermineMessageContextFilter(messageContextFilter, jobId),
                messageBodyFilter: DetermineMessageBodyFilter(messageBodyFilter),
                messageBodySerializer: messageBodySerializer,
                logger: logger);
        }

        private static ProcessMessageAsync DetermineMessageImplementation<TMessage, TMessageContext>(IMessageHandler<TMessage, TMessageContext> messageHandler)
            where TMessageContext : MessageContext
        {
            return async (rawMessage, generalMessageContext, correlationInfo, cancellationToken) =>
            {
                if (rawMessage is not TMessage message)
                {
                    return MessageProcessingResult.Failure(generalMessageContext.MessageId, MessageProcessingError.MatchedHandlerFailed, $"requires message type {typeof(TMessage).Name}");
                }

                if (generalMessageContext is not TMessageContext messageContext)
                {
                    return MessageProcessingResult.Failure(generalMessageContext.MessageId, MessageProcessingError.MatchedHandlerFailed, $"requires message context type {typeof(TMessageContext).Name}");
                }

                await messageHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                return MessageProcessingResult.Success(generalMessageContext.MessageId);
            };
        }

        private static Func<object, MessageHandlerSummary, bool> DetermineMessageBodyFilter<TMessage>(Func<TMessage, bool> messageBodyFilter)
        {
            return (rawMessage, summary) =>
            {
                if (messageBodyFilter is null)
                {
                    return true;
                }

                if (rawMessage is not TMessage message)
                {
                    summary.AddFailed("custom body filter failed", ("requires type", typeof(TMessage).Name));
                    return false;
                }

                bool matches = messageBodyFilter(message);
                if (matches)
                {
                    summary.AddPassed("custom body filter passed", ("against type", typeof(TMessage).Name));
                    return true;
                }

                summary.AddFailed("custom body filter failed", reason: "returns 'false'");
                return false;
            };
        }

        private static Func<MessageContext, MessageHandlerSummary, bool> DetermineMessageContextFilter<TMessageContext>(
            Func<TMessageContext, bool> messageContextFilter,
            string jobId)
            where TMessageContext : MessageContext
        {
            return (rawContext, summary) =>
            {
                ArgumentNullException.ThrowIfNull(rawContext);

                if (jobId is not null && rawContext.JobId != jobId)
                {
                    summary.AddFailed("custom context filter failed", ("requires job ID", rawContext.JobId));
                    return false;
                }

                if (messageContextFilter is null)
                {
                    return true;
                }

                if (rawContext is not TMessageContext messageContext)
                {
                    summary.AddFailed("custom context filter filter failed", ("requires type", typeof(TMessageContext).Name));
                    return false;
                }

                bool matches = messageContextFilter(messageContext);
                if (matches)
                {
                    summary.AddPassed("custom context filter passed", ("against type", typeof(TMessageContext).Name));
                    return true;
                }

                summary.AddFailed("custom context filter failed", reason: "returns 'false'");
                return false;
            };
        }

        /// <summary>
        /// Gets the concrete class type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        [Obsolete("Will be removed in v3.0 in favor of centralizing message handler matching in message router")]
        public object GetMessageHandlerInstance()
        {
            return _messageHandlerInstance;
        }

        /// <summary>
        /// Gets the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        [Obsolete("Will be removed in v3.0 in favor of centralizing message handler matching in message router")]
        public Type GetMessageHandlerType()
        {
            return MessageHandlerType;
        }

        internal bool MatchesMessageContext<TMessageContext>(TMessageContext context, MessageHandlerSummary summary)
            where TMessageContext : MessageContext
        {
            ArgumentNullException.ThrowIfNull(context);
            try
            {
                return _messageContextFilter(context, summary);
            }
            catch (Exception exception)
            {
                summary.AddFailed(exception, "context filter failed");
                return false;
            }
        }

        /// <summary>
        /// Determines if the given <typeparamref name="TMessageContext"/> matches the generic parameter of this message handler.
        /// </summary>
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        /// <param name="messageContext">The context in which the incoming message is processed.</param>
        [Obsolete("Will be removed in v3.0 in favor of centralizing message handler matching in message router")]
        public bool CanProcessMessageBasedOnContext<TMessageContext>(TMessageContext messageContext)
            where TMessageContext : MessageContext
        {
            return MatchesMessageContext(messageContext, new MessageHandlerSummary());
        }

        internal bool MatchesMessageBody(object message, MessageHandlerSummary summary)
        {
            ArgumentNullException.ThrowIfNull(message);
            try
            {
                return _messageBodyFilter.Invoke(message, summary);
            }
            catch (Exception exception)
            {
                summary.AddFailed(exception, "body filter failed");
                return false;
            }
        }

        /// <summary>
        /// Determines if the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the incoming deserialized message based on the consumer-provided message predicate.
        /// </summary>
        /// <param name="message">The incoming deserialized message body.</param>
        [Obsolete("Will be removed in v3.0 in favor of centralizing message handler matching in message router")]
        public bool CanProcessMessageBasedOnMessage(object message)
        {
            return MatchesMessageBody(message, new MessageHandlerSummary());
        }

        internal async Task<MessageResult> TryCustomDeserializeMessageAsync(string message, MessageHandlerSummary summary)
        {
            if (_messageBodySerializer is null)
            {
                return MessageResult.Failure("n/a");
            }

            Task<MessageResult> deserializeMessageAsync = _messageBodySerializer.DeserializeMessageAsync(message);
            Type serializerType = _messageBodySerializer.GetType();

            if (deserializeMessageAsync is null)
            {
                summary.AddFailed("custom body parsing failed", ("using type", serializerType.Name), "returns 'null'");
                return MessageResult.Failure("n/a");
            }

            MessageResult result = await deserializeMessageAsync;
            if (result is null)
            {
                summary.AddFailed("custom body parsing failed", ("using type", serializerType.Name), "returns 'null'");
                return MessageResult.Failure("n/a");
            }

            if (result.IsSuccess)
            {
                Type deserializedMessageType = result.DeserializedMessage.GetType();
                if (deserializedMessageType == MessageType || deserializedMessageType.IsSubclassOf(MessageType))
                {
                    summary.AddPassed("custom body parsing passed", ("using type", serializerType.Name));
                    return result;
                }

                summary.AddFailed("custom body parsing failed", ("using type", serializerType.Name), ("requires type", MessageType.Name), ("but got type", deserializedMessageType.Name));
                return MessageResult.Failure("n/a");
            }

            summary.AddFailed(result.Exception, "custom body parsing failed", result.ErrorMessage);
            return MessageResult.Failure("n/a");
        }

        /// <summary>
        /// Tries to custom deserialize the incoming <paramref name="message"/> via a optional additional <see cref="IMessageBodySerializer"/>
        /// that was provided with the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
        /// </summary>
        /// <param name="message">The incoming message to deserialize.</param>
        /// <returns>
        ///     A <see cref="MessageResult"/> instance that either represents a successful or faulted deserialization of the incoming <paramref name="message"/>.
        /// </returns>
        [Obsolete("Will be removed in v3.0 in favor of centralizing message handler matching in message router")]
        public async Task<MessageResult> TryCustomDeserializeMessageAsync(string message)
        {
            if (_messageBodySerializer is null)
            {
                return MessageResult.Failure("No custom deserialization was found on the registered message handler");
            }

            Task<MessageResult> deserializeMessageAsync = _messageBodySerializer.DeserializeMessageAsync(message);
            if (deserializeMessageAsync is null)
            {
                _logger.LogTrace("Invalid {MessageBodySerializerType} message deserialization was configured on the registered message handler, custom deserialization returned 'null'", nameof(IMessageBodySerializer));
                return MessageResult.Failure("Invalid custom deserialization was configured on the registered message handler");
            }

            MessageResult result = await deserializeMessageAsync;
            if (result is null)
            {
                _logger.LogTrace("No {MessageBodySerializerType} was found on the registered message handler, so no custom deserialization is available", nameof(IMessageBodySerializer));
                return MessageResult.Failure("No custom deserialization was found on the registered message handler");
            }

            if (result.IsSuccess)
            {
                Type deserializedMessageType = result.DeserializedMessage.GetType();
                if (deserializedMessageType == MessageType || deserializedMessageType.IsSubclassOf(MessageType))
                {
                    return result;
                }

                _logger.LogTrace("Incoming message '{DeserializedMessageType}' was successfully custom deserialized but can't be processed by message handler because the handler expects message type '{MessageHandlerMessageType}'; fallback to default deserialization", deserializedMessageType.Name, MessageType.Name);
                return MessageResult.Failure("Custom message deserialization failed because it didn't match the expected message handler's message type");
            }

            if (result.Exception != null)
            {
                _logger.LogError(result.Exception, "Custom {MessageBodySerializerType} message deserialization failed: {ErrorMessage}", nameof(IMessageBodySerializer), result.ErrorMessage);
            }
            else
            {
                _logger.LogError("Custom {MessageBodySerializerType} message deserialization failed: {ErrorMessage}", nameof(IMessageBodySerializer), result.ErrorMessage);
            }

            return MessageResult.Failure("Custom message deserialization failed due to an exception");
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
        /// <returns>
        ///     [true] if the message handler was able to successfully process the message; [false] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler cannot process the message correctly.</exception>
        /// <exception cref="AmbiguousMatchException">Thrown when more than a single processing method was found on the message handler.</exception>
        [Obsolete("Will be removed in v3.0 in favor of centralizing message handler matching in message router")]
        public async Task<bool> ProcessMessageAsync<TMessageContext>(
            object message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken) where TMessageContext : MessageContext
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(messageContext);
            ArgumentNullException.ThrowIfNull(correlationInfo);

            MessageProcessingResult result = await TryProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
            return result.IsSuccessful;
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
        /// <returns>
        ///     [true] if the message handler was able to successfully process the message; [false] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler cannot process the message correctly.</exception>
        internal async Task<MessageProcessingResult> TryProcessMessageAsync<TMessageContext>(
            object message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken) where TMessageContext : MessageContext
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(messageContext);
            ArgumentNullException.ThrowIfNull(correlationInfo);

            const string methodName = nameof(IMessageHandler<object, MessageContext>.ProcessMessageAsync);
            try
            {
                Task<MessageProcessingResult> processMessageAsync =
                    _messageHandlerImplementation(message, messageContext, correlationInfo, cancellationToken);

                if (processMessageAsync is null)
                {
                    throw new InvalidOperationException(
                        $"The '{typeof(IMessageHandler<,>).Name}' implementation '{MessageHandlerType.Name}' returned 'null' while calling the '{methodName}' method");
                }

                return await processMessageAsync;
            }
            catch (AmbiguousMatchException exception)
            {
                return MessageProcessingResult.Failure(messageContext.MessageId, MessageProcessingError.ProcessingInterrupted, $"ambiguous {methodName} method", exception);
            }
            catch (Exception exception)
            {
                return MessageProcessingResult.Failure(messageContext.MessageId, MessageProcessingError.ProcessingInterrupted, "exception thrown", exception);
            }
        }
    }
}
