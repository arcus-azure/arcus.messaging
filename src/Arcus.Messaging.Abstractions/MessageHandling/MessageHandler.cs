﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessMessageAsync = System.Func<object, Arcus.Messaging.Abstractions.MessageContext, Arcus.Messaging.Abstractions.MessageCorrelationInfo, System.Threading.CancellationToken, System.Threading.Tasks.Task<bool>>;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents an abstracted form of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation to handle with different type of generic message and message context types.
    /// </summary>
    public class MessageHandler
    {
        private readonly object _messageHandlerInstance;
        private readonly Type _messageHandlerInstanceType;
        private readonly ProcessMessageAsync _messageHandlerImplementation;
        private readonly IMessageBodySerializer _messageBodySerializer;
        private readonly Func<MessageContext, bool> _messageContextFilter;
        private readonly Func<object, bool> _messageBodyFilter;
        private readonly ILogger _logger;

        private MessageHandler(
            object messageHandlerInstance,
            ProcessMessageAsync messageHandlerImplementation,
            Type messageType,
            Type messageContextType,
            Func<MessageContext, bool> messageContextFilter,
            Func<object, bool> messageBodyFilter,
            IMessageBodySerializer messageBodySerializer,
            ILogger logger)
        {
            Guard.NotNull(messageHandlerInstance, nameof(messageHandlerInstance), "Requires a message handler implementation to apply the message processing filters to");
            Guard.NotNull(messageHandlerImplementation, nameof(messageHandlerImplementation), "Requires a message handler implementation to apply the message processing filters to");
            Guard.NotNull(messageContextFilter, nameof(messageContextFilter), "Requires a message context filter to register with the message handler");
            Guard.NotNull(messageBodyFilter, nameof(messageBodyFilter), "Requires a message body filter to register with the message handler");
            Guard.NotNull(messageType, nameof(messageType), "Requires a message type that the message handler can handle");
            Guard.NotNull(messageContextType, nameof(messageContextType), "Requires a message context type that the message handler can handle");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic messages during the interaction of the message handler");

            _messageHandlerInstance = messageHandlerInstance;
            _messageHandlerInstanceType = messageHandlerInstance.GetType();
            _messageHandlerImplementation = messageHandlerImplementation;
            _messageContextFilter = messageContextFilter;
            _messageBodyFilter = messageBodyFilter;
            _messageBodySerializer = messageBodySerializer;
            _logger = logger;

            MessageType = messageType;
            MessageContextType = messageContextType;
        }

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
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a collection of services to subtract the message handlers from");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write trace messages during the lifetime of the message handlers");

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
        /// <param name="messageBodyFilter">The optional function to filter on the message body before processing.</param>
        /// <param name="messageContextFilter">The optional function to filter on the message context before processing.</param>
        /// <param name="messageBodySerializer">The optional message body serializer instance to customize how the message should be deserialized.</param>
        /// <param name="logger">The logger instance to write diagnostic messages during the message handler interaction.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandler"/> is <c>null</c>.</exception>
        internal static MessageHandler Create<TMessage, TMessageContext>(
            IMessageHandler<TMessage, TMessageContext> messageHandler,
            ILogger<IMessageHandler<TMessage, TMessageContext>> logger,
            Func<TMessage, bool> messageBodyFilter = null,
            Func<TMessageContext, bool> messageContextFilter = null,
            IMessageBodySerializer messageBodySerializer = null) 
            where TMessageContext : MessageContext
        {
            Guard.NotNull(messageHandler, nameof(messageHandler), "Requires a message handler implementation to register the handler within the application services");

            ProcessMessageAsync processMessageAsync = DetermineMessageImplementation(messageHandler);
            logger = logger ?? NullLogger<IMessageHandler<TMessage, TMessageContext>>.Instance;
            Type messageHandlerType = messageHandler.GetType();

            Func<object, bool> messageFilter = DetermineMessageBodyFilter(messageBodyFilter, messageHandlerType, logger);
            Func<MessageContext, bool> contextFilter = DetermineMessageContextFilter(messageContextFilter, messageHandlerType, logger);

            return new MessageHandler(
                messageHandlerInstance: messageHandler,
                messageHandlerImplementation: processMessageAsync,
                messageType: typeof(TMessage),
                messageContextType: typeof(TMessageContext),
                messageContextFilter: contextFilter,
                messageBodyFilter: messageFilter,
                messageBodySerializer: messageBodySerializer,
                logger: logger);
        }

        private static ProcessMessageAsync DetermineMessageImplementation<TMessage, TMessageContext>(IMessageHandler<TMessage, TMessageContext> messageHandler) 
            where TMessageContext : MessageContext
        {
            return async (rawMessage, generalMessageContext, correlationInfo, cancellationToken) =>
            {
                if (rawMessage is TMessage message
                    && generalMessageContext.GetType() == typeof(TMessageContext)
                    && generalMessageContext is TMessageContext messageContext)
                {
                    await messageHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                    return true;
                }

                return false;
            };
        }

        private static Func<object, bool> DetermineMessageBodyFilter<TMessage>(Func<TMessage, bool> messageBodyFilter, Type messageHandlerType, ILogger logger)
        {
           return rawMessage =>
           {
                if (messageBodyFilter is null)
                {
                    return true;
                }

                if (rawMessage is TMessage message)
                {
                    logger.LogTrace("Running message body filter of message handler '{MessageHandler}' for message '{Message}'...", messageHandlerType.Name, typeof(TMessage).Name);
                    bool canProcessMessage = messageBodyFilter(message);
                    logger.LogTrace("Ran message body filter of message handler '{MessageHandler}' for message '{Message}' results in: {Result}", messageHandlerType.Name, typeof(TMessage).Name, canProcessMessage);

                    return canProcessMessage;
                }

                logger.LogTrace("Cannot run message body filter of message handler '{MessageHandler}' for incoming message '{Message}' because the message is of another type '{OtherMessage}'", messageHandlerType.Name, typeof(TMessage).Name, rawMessage.GetType().Name);
                return false;
            };
        }

        private static Func<MessageContext, bool> DetermineMessageContextFilter<TMessageContext>(
            Func<TMessageContext, bool> messageContextFilter,
            Type messageHandlerType,
            ILogger logger)
            where TMessageContext : MessageContext
        {
           return rawContext =>
            {
                if (messageContextFilter is null)
                {
                    return true;
                }

                if (rawContext is TMessageContext messageContext)
                {
                    logger.LogTrace("Running message context filter of message handler '{MessageHandler}' in message context '{MessageContext}'......", messageHandlerType.Name, typeof(TMessageContext).Name);
                    bool canProcessMessage = messageContextFilter(messageContext);
                    logger.LogTrace("Ran message context filter of message handler '{MessageHandler}' for message context '{MessageContext}' results in: {Result}", messageHandlerType.Name, typeof(TMessageContext).Name, canProcessMessage);

                    return canProcessMessage;
                }

                logger.LogTrace("Cannot run message context filter of message handler '{MessageHandler}' for message context '{MessageContext}' because the message is in another context '{OtherMessageContext}'", messageHandlerType, typeof(TMessageContext).Name, rawContext.GetType().Name);
                return false;
            };
        }

        /// <summary>
        /// Gets the concrete class type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        public object GetMessageHandlerInstance()
        {
            return _messageHandlerInstance;
        }

        /// <summary>
        /// Gets the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        public Type GetMessageHandlerType()
        {
            return _messageHandlerInstanceType;
        }

        /// <summary>
        /// Determines if the given <typeparamref name="TMessageContext"/> matches the generic parameter of this message handler.
        /// </summary>
        /// <param name="messageContext">The messaging context information that holds information about the currently processing message.</param>
        /// <returns>
        ///     [true] if the registered <typeparamref name="TMessageContext"/> predicate holds; [false] otherwise.
        /// </returns>
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        [Obsolete("Use the " + nameof(CanProcessMessageBasedOnContext) + " specific message context overload instead")]
        public bool CanProcessMessage<TMessageContext>(TMessageContext messageContext)
            where TMessageContext : MessageContext
        {
            bool canProcessMessageBasedOnContext = CanProcessMessageBasedOnContext(messageContext);
            return canProcessMessageBasedOnContext;
        }

        /// <summary>
        /// Determines if the given <typeparamref name="TMessageContext"/> matches the generic parameter of this message handler.
        /// </summary>
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        /// <param name="messageContext">The context in which the incoming message is processed.</param>
        public bool CanProcessMessageBasedOnContext<TMessageContext>(TMessageContext messageContext) 
            where TMessageContext : MessageContext
        {
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an message context instance to determine if the message handler can process the message");
            try
            {
                return _messageContextFilter.Invoke(messageContext);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to run message context {MessageContextType} predicate for {MessageHandlerType}", messageContext.GetType().Name, _messageHandlerInstanceType.Name);
                return false;
            }
        }

        /// <summary>
        /// Determines if the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the incoming deserialized message based on the consumer-provided message predicate.
        /// </summary>
        /// <param name="message">The incoming deserialized message body.</param>
        public bool CanProcessMessageBasedOnMessage(object message)
        {
            try
            {
                bool canProcessMessage = _messageBodyFilter.Invoke(message);
                return canProcessMessage;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to run message predicate for {MessageHandlerType}", _messageHandlerInstanceType.Name);
                return false;
            }
        }

        /// <summary>
        /// Tries to custom deserialize the incoming <paramref name="message"/> via a optional additional <see cref="IMessageBodySerializer"/>
        /// that was provided with the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
        /// </summary>
        /// <param name="message">The incoming message to deserialize.</param>
        /// <returns>
        ///     A <see cref="MessageResult"/> instance that either represents a successful or faulted deserialization of the incoming <paramref name="message"/>.
        /// </returns>
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
        public async Task<bool> ProcessMessageAsync<TMessageContext>(
            object message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken) where TMessageContext : MessageContext
        {
            Guard.NotNull(message, nameof(message), "Requires message content to deserialize and process the message");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires a message context to send to the message handler");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires correlation information to send to the message handler");

            Type messageType = message.GetType();
            _logger.LogTrace("Start processing '{MessageType}' message in message handler '{MessageHandlerType}'...", messageType, _messageHandlerInstanceType.Name);

            const string methodName = nameof(IMessageHandler<object, MessageContext>.ProcessMessageAsync);
            try
            {
                Task<bool> processMessageAsync = 
                    _messageHandlerImplementation(message, messageContext, correlationInfo, cancellationToken);
                
                if (processMessageAsync is null)
                {
                    throw new InvalidOperationException(
                        $"The '{typeof(IMessageHandler<,>).Name}' implementation '{_messageHandlerInstanceType.Name}' returned 'null' while calling the '{methodName}' method");
                }

                bool isProcessed = await processMessageAsync;
                _logger.LogTrace("Message handler '{MessageHandlerType}' successfully processed '{MessageType}' message", _messageHandlerInstanceType.Name, messageType.Name);

                return isProcessed;
            }
            catch (AmbiguousMatchException exception)
            {
                _logger.LogError(exception, "Ambiguous match found of '{MethodName}' methods in the '{MessageHandlerType}'. Make sure that only 1 matching '{MethodName}' was found on the '{MessageHandlerType}' message handler", methodName, _messageHandlerInstanceType.Name, methodName, _messageHandlerInstanceType.Name);
                return false;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Message handler '{MessageHandlerType}' failed to process '{MessageType}' due to a thrown exception", _messageHandlerInstanceType.Name, messageType.Name);
                return false;
            }
        }
    }
}
