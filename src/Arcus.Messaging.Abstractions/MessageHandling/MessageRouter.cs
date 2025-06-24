using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents how incoming messages gets routed through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.
    /// </summary>
    public abstract class MessageRouter
    {
        private readonly JsonSerializerOptions _jsonOptions;

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
