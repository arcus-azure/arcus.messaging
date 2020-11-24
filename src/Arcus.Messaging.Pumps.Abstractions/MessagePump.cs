using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    ///     Foundation for building message pumps
    /// </summary>
    public abstract class MessagePump : BackgroundService
    {
        private readonly Lazy<IEnumerable<MessageHandler>> _messageHandlers;
        private readonly IFallbackMessageHandler _fallbackMessageHandler;

        /// <summary>
        ///     Default encoding used
        /// </summary>
        protected Encoding DefaultEncoding { get; } = Encoding.UTF8;

        /// <summary>
        ///     Unique id of this message pump instance
        /// </summary>
        protected string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     Id of the client being used to connect to the messaging service
        /// </summary>
        protected string ClientId { get; private set; }

        /// <summary>
        ///     Entity path that is being processed
        /// </summary>
        protected string EntityPath { get; private set; }

        /// <summary>
        ///     Logger to write telemetry to
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        ///     Configuration of the application
        /// </summary>
        protected IConfiguration Configuration { get; }


        /// <summary>
        ///     Collection of services that are configured
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        protected MessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            Logger = logger;
            Configuration = configuration;
            ServiceProvider = serviceProvider;

            _messageHandlers = new Lazy<IEnumerable<MessageHandler>>(() => MessageHandler.SubtractFrom(ServiceProvider, logger));
            _fallbackMessageHandler = serviceProvider.GetService<IFallbackMessageHandler>();
        }

        /// <summary>
        ///     Handles an exception that occured during the receiving of a message
        /// </summary>
        /// <param name="receiveException">Exception that occured</param>
        protected virtual Task HandleReceiveExceptionAsync(Exception receiveException)
        {
            Logger.LogCritical(receiveException, "Unable to process message from {EntityPath} with client {ClientId}", EntityPath, ClientId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Pre-process the message by setting the necessary values the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation.
        /// </summary>
        /// <param name="messageHandler">The message handler to be used to process the message.</param>
        /// <param name="messageContext">The message context of the message that will be handled.</param>
        protected virtual Task PreProcessMessageAsync<TMessageContext>(MessageHandler messageHandler, TMessageContext messageContext) 
            where TMessageContext : MessageContext
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Handle a new message that was received
        /// </summary>
        /// <typeparam name="TMessageContext">Type of message context for the provider</typeparam>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        protected async Task<MessageHandlerResult> ProcessMessageAndCaptureAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            Guard.NotNull(message, nameof(message), "Requires message content to deserialize and process the message");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires a message context to send to the message handler");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires correlation information to send to the message handler");

            try
            {
                bool isProcessed = await TryProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                if (isProcessed)
                {
                    return MessageHandlerResult.Success();
                }

                return MessageHandlerResult.Pending();
            }
            catch (Exception exception)
            {
                return MessageHandlerResult.Failure(exception);
            }
        }

        /// <summary>
        ///     Handle a new message that was received
        /// </summary>
        /// <typeparam name="TMessageContext">Type of message context for the provider</typeparam>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        protected async Task ProcessMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            Guard.NotNull(message, nameof(message), "Requires message content to deserialize and process the message");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires a message context to send to the message handler");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires correlation information to send to the message handler");

            bool isProcessed = await TryProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
            if (isProcessed)
            {
                return;
            }

            await FallbackProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
        }

        private async Task<bool> TryProcessMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(ServiceProvider, Logger);
            if (!handlers.Any() && _fallbackMessageHandler is null)
            {
                throw new InvalidOperationException(
                    $"Message pump cannot correctly process the message in the '{typeof(TMessageContext)}' "
                    + "because no 'IMessageHandler<,>' was registered in the dependency injection container. "
                    + $"Make sure you call the correct '.With...' extension on the {nameof(IServiceCollection)} during the registration of the message pump to register a message handler");
            }

            foreach (MessageHandler handler in handlers)
            {
                bool isProcessed = await ProcessMessageAsync(handler, message, messageContext, correlationInfo, cancellationToken);
                if (isProcessed)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> ProcessMessageAsync<TMessageContext>(
            MessageHandler handler, 
            string message, 
            TMessageContext messageContext, 
            MessageCorrelationInfo correlationInfo, 
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            Type messageHandlerType = handler.GetMessageHandlerType();
            Logger.LogTrace("Determine if message handler '{MessageHandlerType}' can process the message...", messageHandlerType.Name);
            
            bool canProcessMessage = handler.CanProcessMessage(messageContext);
            if (canProcessMessage)
            {
                MessageResult messageResult = await DeserializeMessageAsync(message, handler.MessageType);
                if (messageResult.IsSuccess)
                {
                    await PreProcessMessageAsync(handler, messageContext);
                    await handler.ProcessMessageAsync(messageResult.DeserializedMessage, messageContext, correlationInfo, cancellationToken);
                    return true;
                }

                Logger.LogTrace(
                    "Message handler '{MessageHandlerType}' is not able to process the message because the incoming message cannot be deserialized to the message that the message handler can handle",
                    messageHandlerType.Name);
            }
            else 
            {
                Logger.LogTrace(
                    "Message handler '{MessageHandlerType}' is not able to process the message because the message context '{MessageContextType}' didn't match the correct message handler's message context",
                    messageHandlerType.Name, handler.MessageContextType.Name);
            }

            return false;
        }

        private async Task<MessageResult> DeserializeMessageAsync(string message, Type messageType)
        {
            IEnumerable<IMessageBodySerializer> messageBodyHandlers = ServiceProvider.GetServices<IMessageBodySerializer>();

            foreach (IMessageBodySerializer messageBodyHandler in messageBodyHandlers)
            {
                MessageResult result = await messageBodyHandler.DeserializeMessageAsync(message);
                if (result.IsSuccess && result.DeserializedMessage.GetType() == messageType)
                {
                    return result;
                }
            }

            if (TryDeserializeToMessageFormat(message, messageType, out object? deserializedByType) && deserializedByType != null)
            {
                return MessageResult.Success(deserializedByType);
            }

            return MessageResult.Failure($"Incoming message cannot be deserialized to type '{messageType.Name}' because it is not in the correct format");
        }

        private async Task FallbackProcessMessageAsync<TMessageContext>(
            string message, 
            TMessageContext messageContext, 
            MessageCorrelationInfo correlationInfo, 
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            if (_fallbackMessageHandler is null)
            {
                throw new InvalidOperationException(
                    $"Message pump cannot correctly process the message in the '{typeof(TMessageContext)}' "
                    + "because none of the registered 'IMessageHandler<,>' implementations in the dependency injection container matches the incoming message type and context. "
                    + $"Make sure you call the correct '.With...' extension on the {nameof(IServiceCollection)} during the registration of the message pump to register a message handler");
            }

            Logger.LogInformation(
                "Fallback on registered {FallbackMessageHandlerType} because none of the message handlers were able to process the message",
                nameof(IFallbackMessageHandler));

            Task processMessageAsync = _fallbackMessageHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
            if (processMessageAsync is null)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(IFallbackMessageHandler)}' was not correctly implemented to process the message");
            }

            await processMessageAsync;

            Logger.LogTrace("Fallback message handler has processed the message");
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
        public virtual bool TryDeserializeToMessageFormat(string message, Type messageType, out object? result)
        {
            Guard.NotNullOrWhitespace(message, nameof(message), "Can't parse a blank raw message against a message handler's contract");

            Logger.LogTrace("Try to JSON deserialize incoming message to message type '{MessageType}'...", messageType.Name);

            var success = true;
            var jsonSerializer = new JsonSerializer
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };
            jsonSerializer.Error += (sender, args) =>
            {
                success = false;
                args.ErrorContext.Handled = true;
            };

            var value = JToken.Parse(message).ToObject(messageType, jsonSerializer);
            if (success)
            {
                Logger.LogTrace("Incoming message was successfully JSON deserialized to message type '{MessageType}'", messageType.Name);

                result = value;
                return true;
            }

            Logger.LogTrace("Incoming message failed to be JSON deserialized to message type '{MessageType}'", messageType.Name);

            result = null;
            return false;
        }

        /// <summary>
        ///     Triggered when the message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogWarning("Host is shutting down");

            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        ///     Register information about the client connected to the messaging service
        /// </summary>
        /// <param name="clientId">Id of the client being used to connect to the messaging service</param>
        /// <param name="entityPath">Entity path that is being processed</param>
        protected void RegisterClientInformation(string clientId, string entityPath)
        {
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));

            ClientId = clientId;
            EntityPath = entityPath;
        }
    }
}