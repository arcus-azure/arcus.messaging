using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Context;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents how incoming messages gets routed through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete: deprecated interface will be removed in v3.0.
    public class MessageRouter : IMessageRouter
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        public MessageRouter(IServiceProvider serviceProvider, MessageRouterOptions options, ILogger<MessageRouter> logger)
            : this(serviceProvider, options, (ILogger) logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        public MessageRouter(IServiceProvider serviceProvider, MessageRouterOptions options)
            : this(serviceProvider, options, NullLogger.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        public MessageRouter(IServiceProvider serviceProvider, ILogger<MessageRouter> logger)
            : this(serviceProvider, new MessageRouterOptions(), (ILogger) logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        public MessageRouter(IServiceProvider serviceProvider)
            : this(serviceProvider, new MessageRouterOptions(), NullLogger.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        protected MessageRouter(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, new MessageRouterOptions(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected MessageRouter(IServiceProvider serviceProvider, MessageRouterOptions options, ILogger logger)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Options = options ?? new MessageRouterOptions();
            Logger = logger ?? NullLogger<MessageRouter>.Instance;

            _jsonOptions = CreateJsonSerializerOptions(Options.Deserialization, Logger);
        }

        private static JsonSerializerOptions CreateJsonSerializerOptions(MessageDeserializationOptions deserializationOptions, ILogger logger)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                UnmappedMemberHandling = deserializationOptions?.AdditionalMembers switch
                {
                    null => JsonUnmappedMemberHandling.Disallow,
                    AdditionalMemberHandling.Error => JsonUnmappedMemberHandling.Disallow,
                    AdditionalMemberHandling.Ignore => JsonUnmappedMemberHandling.Skip,
                    _ => JsonUnmappedMemberHandling.Disallow
                }
            };

            logger.LogTrace("JSON deserialization uses '{UnmappedMemberHandling}' result when encountering additional members", jsonOptions.UnmappedMemberHandling);
            return jsonOptions;
        }

        /// <summary>
        /// Gets the instance that provides all the registered services in the current application.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the router.
        /// </summary>
        protected MessageRouterOptions Options { get; }

        /// <summary>
        /// Gets the logger instance that writes diagnostic trace messages during the routing of the messages.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        /// <returns>
        ///     [true] if the router was able to process the message through one of the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>s; [false] otherwise.
        /// </returns>
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message routing will be supported from now on")]
        protected async Task<bool> RouteMessageWithoutFallbackAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            if (correlationInfo is null)
            {
                throw new ArgumentNullException(nameof(correlationInfo));
            }

            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            using (LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfo, Options.CorrelationEnricher)))
            {
                var accessor = serviceScope.ServiceProvider.GetService<IMessageCorrelationInfoAccessor>();
                accessor?.SetCorrelationInfo(correlationInfo);

                bool isProcessed = await TryProcessMessageAsync(serviceScope.ServiceProvider, message, messageContext, correlationInfo, cancellationToken);
                return isProcessed;
            }
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message routing will be supported from now on")]
        public virtual async Task RouteMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            if (correlationInfo is null)
            {
                throw new ArgumentNullException(nameof(correlationInfo));
            }

            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            using (LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfo, Options.CorrelationEnricher)))
            {
                var accessor = serviceScope.ServiceProvider.GetService<IMessageCorrelationInfoAccessor>();
                accessor?.SetCorrelationInfo(correlationInfo);

                await RouteMessageAsync(serviceScope.ServiceProvider, message, messageContext, correlationInfo, cancellationToken);
            }
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="serviceProvider">The available services from which the message handlers should be retrieved.</param>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message routing will be supported from now on")]
        protected async Task RouteMessageAsync<TMessageContext>(
            IServiceProvider serviceProvider,
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            bool isProcessed = await TryProcessMessageAsync(serviceProvider, message, messageContext, correlationInfo, cancellationToken);
            if (isProcessed)
            {
                return;
            }

            bool isFallbackProcessed = await TryFallbackProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
            if (!isFallbackProcessed)
            {
                Logger.LogDebug("Message router cannot correctly process the message in the '{MessageContextType}' because none of the registered '{MessageHandlerType}' implementations in the dependency container matches the incoming message type and context. Make sure you call the correct '.With...' extension on the '{ServiceCollectionType}' during the registration of the message pump/router to register a message handler", typeof(TMessageContext).Name, typeof(IMessageHandler<,>).Name, nameof(IServiceCollection));
            }
        }

        private async Task<bool> TryProcessMessageAsync<TMessageContext>(
            IServiceProvider serviceProvider,
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            MessageHandler[] handlers = GetRegisteredMessageHandlers(serviceProvider).ToArray();
            FallbackMessageHandler<string, TMessageContext>[] fallbackHandlers =
                GetAvailableFallbackMessageHandlersByContext<string, TMessageContext>(messageContext);

            if (handlers.Length <= 0 && fallbackHandlers.Length <= 0)
            {
                throw new InvalidOperationException(
                    $"Message pump cannot correctly process the message in the '{typeof(TMessageContext).Name}' "
                    + "because no 'IMessageHandler<,>' implementations were registered in the dependency injection container. "
                    + $"Make sure you call the correct '.With...' extension on the {nameof(IServiceCollection)} during the registration of the message pump or message router to register a message handler");
            }

            foreach (MessageHandler handler in handlers)
            {
                MessageResult result = await DeserializeMessageForHandlerAsync(message, messageContext, handler);
                if (result.IsSuccess)
                {
                    bool isProcessed = await handler.ProcessMessageAsync(result.DeserializedMessage, messageContext, correlationInfo, cancellationToken);
                    if (!isProcessed)
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances in the application.
        /// </summary>
        /// <param name="serviceProvider">The scoped service provider from which the registered message handlers will be extracted.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected IEnumerable<MessageHandler> GetRegisteredMessageHandlers(IServiceProvider serviceProvider)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(serviceProvider, Logger);
            return handlers;
        }

        /// <summary>
        /// Deserializes the incoming <paramref name="message"/> within the <paramref name="messageContext"/> for a specific <paramref name="handler"/>.
        /// </summary>
        /// <param name="message">The incoming message that needs to be deserialized so it can be processed by the <paramref name="handler"/>.</param>
        /// <param name="messageContext">The message context in which the <paramref name="message"/> was received.</param>
        /// <param name="handler">The specific <see cref="IMessageHandler{TMessage,TMessageContext}"/> for which the incoming <paramref name="message"/> should be deserialized.</param>
        /// <returns>
        ///     [<see cref="MessageResult.Success"/>] when the incoming <paramref name="message"/> within the <paramref name="messageContext"/> was successfully deserialized for the <paramref name="handler"/>
        ///     and all the required predicates that the <paramref name="handler"/> requires are met; [<see cref="MessageResult.Failure(string)"/>] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handler"/> is <c>null</c>.</exception>
        protected async Task<MessageResult> DeserializeMessageForHandlerAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageHandler handler)
            where TMessageContext : MessageContext
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type messageHandlerType = handler.GetMessageHandlerType();
            Logger.LogTrace("Determine if message handler '{MessageHandlerType}' can process the message...", messageHandlerType.Name);

            bool canProcessMessageBasedOnContext = handler.CanProcessMessageBasedOnContext(messageContext);
            if (!canProcessMessageBasedOnContext)
            {
                Logger.LogTrace("Message handler '{MessageHandlerType}' is not able to process the message because the message context '{MessageContextType}' didn't match the correct message handler's message context and/or filter", messageHandlerType.Name, handler.MessageContextType.Name);
                return MessageResult.Failure($"Message handler '{messageHandlerType.Name}' can't process message with message context '{handler.MessageContextType.Name}'");
            }

            MessageResult deserializationResult = await DeserializeMessageAsync(handler, message, handler.MessageType);
            if (!deserializationResult.IsSuccess)
            {
                Logger.LogTrace("Message handler '{MessageHandlerType}' is not able to process the message because the incoming message cannot be deserialized to the message '{MessageType}' that the message handler can handle", messageHandlerType.Name, handler.MessageType.Name);
                return MessageResult.Failure($"Message handler '{messageHandlerType.Name}' can't process message because it can't be deserialized to message type '{handler.MessageType.Name}'");
            }

            bool canProcessDeserializedMessage = handler.CanProcessMessageBasedOnMessage(deserializationResult.DeserializedMessage);
            if (canProcessDeserializedMessage)
            {
                return deserializationResult;
            }

            Logger.LogTrace("Message handler '{MessageHandlerType}' is not able to process the message because the incoming message '{MessageType}' doesn't match the registered message filter", messageHandlerType.Name, handler.MessageType.Name);
            return MessageResult.Failure($"Message handler '{messageHandlerType.Name}' can't process message because it fails the '{handler.MessageType.Name}' filter");
        }

        private async Task<MessageResult> DeserializeMessageAsync(MessageHandler handler, string message, Type handlerMessageType)
        {
            MessageResult result = await handler.TryCustomDeserializeMessageAsync(message);
            if (result.IsSuccess)
            {
                return result;
            }

            if (TryDeserializeToMessageFormat(message, handlerMessageType, out object deserializedByType) && deserializedByType != null)
            {
                return MessageResult.Success(deserializedByType);
            }

            return MessageResult.Failure($"Incoming message cannot be deserialized to type '{handlerMessageType.Name}' because it is not in the correct format");
        }

        /// <summary>
        /// Handle the original <paramref name="message"/> that was received through an registered <see cref="IFallbackMessageHandler"/>.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        ///     [true] if the received <paramref name="message"/> was handled by the registered <see cref="IFallbackMessageHandler"/>; [false] otherwise.
        /// </returns>
        [Obsolete("Will be removed in v3.0 as only concrete implementations of message routing will be supported from now on")]
        protected async Task<bool> TryFallbackProcessMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            if (correlationInfo is null)
            {
                throw new ArgumentNullException(nameof(correlationInfo));
            }

            bool isProcessedByTypedContext = await TryFallbackProcessMessageByContextAsync(message, messageContext, correlationInfo, cancellationToken);
            if (isProcessedByTypedContext)
            {
                return true;
            }

            bool isProcessedByGeneralContext = await TryFallbackProcessMessageByContextAsync<MessageContext>(message, messageContext, correlationInfo, cancellationToken);
            return isProcessedByGeneralContext;
        }

        private async Task<bool> TryFallbackProcessMessageByContextAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            FallbackMessageHandler<string, TMessageContext>[] fallbackHandlers =
                GetAvailableFallbackMessageHandlersByContext<string, TMessageContext>(messageContext);

            if (fallbackHandlers.Length <= 0)
            {
                Logger.LogTrace("No fallback message handlers found within message context '{Type}' (JobId: {JobId})", typeof(TMessageContext).Name, messageContext.JobId);
                return false;
            }

            foreach (FallbackMessageHandler<string, TMessageContext> fallbackHandler in fallbackHandlers)
            {
                string fallbackMessageHandlerTypeName = fallbackHandler.MessageHandlerType.Name;

                Logger.LogTrace("Fallback on registered '{FallbackMessageHandlerType}' because none of the general message handlers were able to process the message", fallbackMessageHandlerTypeName);

                try
                {
                    Task<bool> processMessageAsync = fallbackHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                    if (processMessageAsync is null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot fallback upon the general fallback message handler '{fallbackMessageHandlerTypeName}' "
                            + "because the handler was not correctly implemented to process the message as it returns 'null' for its asynchronous operation");
                    }

                    bool result = await processMessageAsync;
                    if (result)
                    {
                        Logger.LogTrace("Fallback message handler '{FallbackMessageHandlerType}' has processed the message", fallbackMessageHandlerTypeName);
                        return true;
                    }

                    Logger.LogTrace("Fallback message handler '{FallbackMessageHandlerType}' was not able to process the message", fallbackMessageHandlerTypeName);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Fallback message handler '{FallbackMessageHandlerType}' was not able to process the message: {Message}", fallbackMessageHandlerTypeName, exception.Message);
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all the available fallback message handlers for this <paramref name="messageContext"/> registered in the application services.
        /// </summary>
        /// <param name="messageContext">The message context in which the message is received and where the fallback message handler should take up.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageContext"/> is <c>null</c>.</exception>
        protected FallbackMessageHandler<TMessage, TMessageContext>[] GetAvailableFallbackMessageHandlersByContext<TMessage, TMessageContext>(
            TMessageContext messageContext)
            where TMessage : class
            where TMessageContext : MessageContext
        {
            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            return ServiceProvider.GetServices<FallbackMessageHandler<TMessage, TMessageContext>>()
                                  .Where(handler => handler.CanProcessMessageBasedOnContext(messageContext))
                                  .ToArray();
        }

        /// <summary>
        /// Tries to parse the given raw <paramref name="message"/> to the contract of the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
        /// </summary>
        /// <param name="message">The raw incoming message that will be tried to parse against the <see cref="IMessageHandler{TMessage,TMessageContext}"/>'s message contract.</param>
        /// <param name="messageType">The type of the message that the message handler can process.</param>
        /// <param name="result">The resulted parsed message when the <paramref name="message"/> conforms with the message handlers' contract.</param>
        /// <returns>
        ///     [true] if the <paramref name="message"/> conforms the <see cref="IMessageHandler{TMessage,TMessageContext}"/>'s contract; otherwise [false].
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messageType"/> is blank.</exception>
        protected virtual bool TryDeserializeToMessageFormat(string message, Type messageType, out object result)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Requires a non-blank message to deserialize to a given type", nameof(message));
            }

            Logger.LogTrace("Try to JSON deserialize incoming message to message type '{MessageType}'...", messageType.Name);
            try
            {
                result = JsonSerializer.Deserialize(message, messageType, _jsonOptions);
                if (result != null)
                {
                    Logger.LogTrace("Incoming message was successfully JSON deserialized to message type '{MessageType}'", messageType.Name);
                    return true;
                }
            }
            catch (JsonException exception)
            {
                Logger.LogTrace(exception, "Incoming message failed to be JSON deserialized to message type '{MessageType}' due to an exception", messageType.Name);
            }

            result = null;
            return false;
        }
    }
}
