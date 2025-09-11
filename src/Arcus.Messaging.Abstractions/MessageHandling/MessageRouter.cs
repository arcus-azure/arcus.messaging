using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Arcus.Messaging.Abstractions.MessageHandling.MessageRouterLoggerExtensions;

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

            _jsonOptions = new JsonSerializerOptions
            {
                UnmappedMemberHandling = Options.Deserialization?.AdditionalMembers switch
                {
                    null => JsonUnmappedMemberHandling.Disallow,
                    AdditionalMemberHandling.Error => JsonUnmappedMemberHandling.Disallow,
                    AdditionalMemberHandling.Ignore => JsonUnmappedMemberHandling.Skip,
                    _ => JsonUnmappedMemberHandling.Disallow
                }
            };
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
        [Obsolete("Will be removed in v3.0 in favor of centralizing message routing functionality, please use the " + nameof(RouteMessageThroughRegisteredHandlersAsync))]
        protected IEnumerable<MessageHandler> GetRegisteredMessageHandlers(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(serviceProvider, Logger);
            return handlers;
        }

        /// <summary>
        /// Routes the incoming <paramref name="messageBody"/> within the <paramref name="messageContext"/>
        /// through all the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances registered in the application <paramref name="services"/>.
        /// </summary>
        /// <typeparam name="TMessageContext">The custom type of the message context in which the message gets handled.</typeparam>
        /// <param name="services">The current application services with the registered message handlers.</param>
        /// <param name="messageBody">The raw message body to be deserialized to a message type that a message handler can accept.</param>
        /// <param name="messageContext">The context in which the message gets processed.</param>
        /// <param name="correlationInfo">The additional service-to-service correlation scope in which this message is a part of.</param>
        /// <param name="cancellation">The token to cancel the message handling process.</param>
        /// <returns>
        ///     <para>[Success] when one of the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances successfully processed the incoming <paramref name="messageBody"/>;</para>
        ///     <para>[Failure] otherwise, with additional information about the failure.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when one of the arguments is <c>null</c>.</exception>
        protected async Task<MessageProcessingResult> RouteMessageThroughRegisteredHandlersAsync<TMessageContext>(
            IServiceProvider services,
            string messageBody,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellation)
            where TMessageContext : MessageContext
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(messageBody);
            ArgumentNullException.ThrowIfNull(messageContext);
            ArgumentNullException.ThrowIfNull(correlationInfo);

            MessageHandler[] handlers = MessageHandler.SubtractFrom(services, Logger).ToArray();
            if (handlers.Length <= 0)
            {
                return NoHandlersRegistered(messageContext.MessageId);
            }

            try
            {
                int skippedHandlers = 0;
                bool hasGoneThroughMessageHandler = false;

                foreach (var handler in handlers)
                {
                    MessageProcessingResult result = await ProcessMessageHandlerAsync(handler, messageBody, messageContext, correlationInfo, cancellation);
                    hasGoneThroughMessageHandler = result.IsSuccessful || result.Error is MessageProcessingError.ProcessingInterrupted;

                    if (result.IsSuccessful)
                    {
                        Logger.LogMessageProcessedSummary(messageContext.MessageId, handler.MessageHandlerType, skippedHandlers);
                        return result;
                    }

                    skippedHandlers++;
                }

                return hasGoneThroughMessageHandler
                    ? MatchedHandlerFailed(messageContext.MessageId)
                    : NoMatchedHandler(messageContext.MessageId);
            }
            catch (Exception exception)
            {
                return ExceptionDuringRouting(exception, messageContext.MessageId);
            }

            MessageProcessingResult NoHandlersRegistered(string messageId)
            {
                Logger.LogNoHandlersRegistered(messageId);
                return MessageProcessingResult.Failure(messageId, MessageProcessingError.CannotFindMatchedHandler, NoHandlersRegisteredMessage);
            }

            MessageProcessingResult NoMatchedHandler(string messageId)
            {
                Logger.LogNoMatchedMessageHandler(messageId);
                return MessageProcessingResult.Failure(messageId, MessageProcessingError.CannotFindMatchedHandler, NoMatchedHandlerMessage);
            }

            MessageProcessingResult MatchedHandlerFailed(string messageId)
            {
                Logger.LogMatchedHandlerFailedToProcessMessage(messageId);
                return MessageProcessingResult.Failure(messageId, MessageProcessingError.MatchedHandlerFailed, MatchedHandlerFailedMessage);
            }

            MessageProcessingResult ExceptionDuringRouting(Exception exception, string messageId)
            {
                Logger.LogExceptionDuringRouting(exception, messageId);
                return MessageProcessingResult.Failure(messageId, MessageProcessingError.ProcessingInterrupted, ExceptionDuringRoutingMessage, exception);
            }
        }

        private async Task<MessageProcessingResult> ProcessMessageHandlerAsync<TMessageContext>(
            MessageHandler handler,
            string messageBody,
            TMessageContext context,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellation)
            where TMessageContext : MessageContext
        {
            using var _ = Logger.BeginScope(new Dictionary<string, object> { ["JobId"] = context.JobId });
            var summary = new MessageHandlerSummary();

            if (!handler.MatchesMessageContext(context, summary))
            {
                return MatchedHandlerSkipped();
            }

            MessageResult bodyResult = await DeserializeMessageAsync(handler, messageBody, handler.MessageType, summary);
            if (!bodyResult.IsSuccess)
            {
                return MatchedHandlerSkipped();
            }

            if (!handler.MatchesMessageBody(bodyResult.DeserializedMessage, summary))
            {
                return MatchedHandlerSkipped();
            }

            Type messageType = bodyResult.DeserializedMessage.GetType();

            MessageProcessingResult processResult = await handler.TryProcessMessageAsync(bodyResult.DeserializedMessage, context, correlationInfo, cancellation);
            if (!processResult.IsSuccessful)
            {
                return MatchedHandlerFailed(processResult.ProcessingException, processResult.ErrorMessage);
            }

            Logger.LogMessageProcessedByHandler(messageType, context.MessageId, handler.MessageHandlerType, summary);
            return MessageProcessingResult.Success(context.MessageId);

            MessageProcessingResult MatchedHandlerSkipped()
            {
                Logger.LogMessageSkippedByHandler(context.MessageId, handler.MessageHandlerType, summary);
                return MessageProcessingResult.Failure(context.MessageId, MessageProcessingError.MatchedHandlerFailed, "n/a");
            }

            MessageProcessingResult MatchedHandlerFailed(Exception exception, string errorMessage)
            {
                summary.AddFailed(exception, "message processing failed", errorMessage);

                Logger.LogMessageFailedInHandler(messageType, context.MessageId, handler.MessageHandlerType, summary);
                return MessageProcessingResult.Failure(context.MessageId, MessageProcessingError.ProcessingInterrupted, "n/a");
            }
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
        [Obsolete("Will be removed in v3.0 in favor of centralizing message routing functionality, please use the " + nameof(RouteMessageThroughRegisteredHandlersAsync))]
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

        private async Task<MessageResult> DeserializeMessageAsync(MessageHandler handler, string messageBody, Type handlerMessageType, MessageHandlerSummary summary = null)
        {
            summary ??= new MessageHandlerSummary();

            MessageResult result = await handler.TryCustomDeserializeMessageAsync(messageBody, summary);
            if (result.IsSuccess)
            {
                return result;
            }

            try
            {
                object deserializedByType = JsonSerializer.Deserialize(messageBody, handlerMessageType, _jsonOptions);
                if (deserializedByType != null)
                {
                    summary.AddPassed("default JSON body parsing passed", ("additional members", Options.Deserialization.AdditionalMembers));
                    return MessageResult.Success(deserializedByType);
                }

                summary.AddFailed("default JSON body parsing failed", reason: "returns 'null'");
                return MessageResult.Failure("n/a");
            }
            catch (JsonException exception)
            {
                summary.AddFailed(exception, "default JSON body parsing failed");
                return MessageResult.Failure(exception);
            }
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
        [Obsolete("Will be removed in v3.0 in favor of centralizing message routing, use the " + nameof(RouteMessageThroughRegisteredHandlersAsync) + " instead")]
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

    /// <summary>
    /// Extensions on the <see cref="ILogger"/> for centralizing and more easily streamlining the log messages during the message routing.
    /// </summary>
    internal static partial class MessageRouterLoggerExtensions
    {
        internal const string NoHandlersRegisteredMessage = "no message handlers registered in application services",
                              NoMatchedHandlerMessage = "no matched handler handled found for message",
                              MatchedHandlerFailedMessage = "matched message handler failed to process message",
                              ExceptionDuringRoutingMessage = "unexpected critical problem during message processing";

        [LoggerMessage(LogLevel.Error, "Message '{MessageId}' [Failed in] message pump => ✗ " + NoHandlersRegisteredMessage)]
        internal static partial void LogNoHandlersRegistered(this ILogger logger, string messageId);

        [LoggerMessage(LogLevel.Error, "Message '{MessageId}' [Failed in] message pump => ✗ " + NoMatchedHandlerMessage)]
        internal static partial void LogNoMatchedMessageHandler(this ILogger logger, string messageId);

        [LoggerMessage(LogLevel.Error, "Message '{MessageId}' [Failed in] message pump => ✗ " + MatchedHandlerFailedMessage)]
        internal static partial void LogMatchedHandlerFailedToProcessMessage(this ILogger logger, string messageId);

        [LoggerMessage(LogLevel.Critical, "Message '{MessageId}' [Failed in] message pump => ✗ " + ExceptionDuringRoutingMessage)]
        internal static partial void LogExceptionDuringRouting(this ILogger logger, Exception exception, string messageId);

        internal static void LogMessageSkippedByHandler(this ILogger logger, string messageId, Type messageHandlerType, MessageHandlerSummary summary)
        {
            LogMessageSkippedByHandler(logger, summary.OccurredException, messageId, messageHandlerType.Name, summary);
        }

        [LoggerMessage(LogLevel.Debug, "Message {MessageId} [Skipped by] {MessageHandlerType} => {Summary}")]
        internal static partial void LogMessageSkippedByHandler(this ILogger logger, Exception exception, string messageId, string messageHandlerType, MessageHandlerSummary summary);

        internal static void LogMessageFailedInHandler(this ILogger logger, Type messageType, string messageId, Type messageHandlerType, MessageHandlerSummary summary)
        {
            LogMessageFailedInHandler(logger, summary.OccurredException, messageType.Name, messageId, messageHandlerType.Name, summary);
        }

        [LoggerMessage(LogLevel.Error, "{MessageType} {MessageId} [Failed in] {MessageHandlerType} => {Summary}")]
        internal static partial void LogMessageFailedInHandler(this ILogger logger, Exception exception, string messageType, string messageId, string messageHandlerType, MessageHandlerSummary summary);

        internal static void LogMessageProcessedByHandler(this ILogger logger, Type messageType, string messageId, Type messageHandlerType, MessageHandlerSummary summary)
        {
            LogMessageProcessedByHandler(logger, messageType.Name, messageId, messageHandlerType.Name, summary);
        }

        [LoggerMessage(LogLevel.Debug, "{MessageType} {MessageId} [Processed by] {MessageHandlerType} => {Summary}")]
        internal static partial void LogMessageProcessedByHandler(this ILogger logger, string messageType, string messageId, string messageHandlerType, MessageHandlerSummary summary);

        internal static void LogMessageProcessedSummary(this ILogger logger, string messageId, Type messageHandlerType, int skippedHandlers)
        {
            string skippedHandlersDescription = skippedHandlers switch
            {
                0 => "no other handlers",
                1 => "1 other handler",
                _ => skippedHandlers + " other handlers"
            };

            LogMessageProcessedSummary(logger, messageId, messageHandlerType.Name, skippedHandlersDescription);
        }

        [LoggerMessage(LogLevel.Information, "Message '{MessageId}' was processed by {MessageHandlerType} | skipped by {SkippedHandlers}")]
        internal static partial void LogMessageProcessedSummary(this ILogger logger, string messageId, string messageHandlerType, string skippedHandlers);
    }

    /// <summary>
    /// Represents a summary of all the pre-checks that were performed
    /// during the processing of a message by a specific <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
    /// </summary>
    internal class MessageHandlerSummary
    {
        private readonly Collection<string> _lines = [];
        private readonly Collection<Exception> _exceptions = [];

        /// <summary>
        /// Gets the possible exception(s) that occurred during the pre-checks.
        /// </summary>
        internal Exception OccurredException => _exceptions.Count switch
        {
            0 => null,
            1 => _exceptions.Single(),
            _ => new AggregateException(_exceptions)
        };

        /// <summary>
        /// Adds a passed check to the summary.
        /// </summary>
        /// <param name="description">The message that describes in a short manner the check that was passed.</param>
        /// <param name="members">The additional key-value pair members that were involved in the check.</param>
        internal void AddPassed(string description, params (string, object)[] members)
        {
            ArgumentNullException.ThrowIfNull(description);

            string membersDescription = members.Length > 0
                ? $" ({string.Join(", ", members.Select(m => m.Item1 + "=" + m.Item2))})"
                : string.Empty;

            _lines.Add("✓ " + description + membersDescription);
        }

        /// <summary>
        /// Adds a failed check to the summary.
        /// </summary>
        /// <param name="exception">The optional exception that occured during the check.</param>
        /// <param name="description">The message that describes in a short manner the check that was failed.</param>
        /// <param name="reason">The message that explains the reason why the check failed.</param>
        internal void AddFailed(Exception exception, string description, string reason = "exception thrown")
        {
            if (exception is not null)
            {
                _exceptions.Add(exception);
            }

            AddFailed(description, reason);
        }

        /// <summary>
        /// Adds a failed check to the summary.
        /// </summary>
        /// <param name="description">The message that describes in a short manner the check that was failed.</param>
        /// <param name="reason">The message that explains the reason why the check failed.</param>
        internal void AddFailed(string description, string reason)
        {
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(reason);

            AddFailed(description + " (" + reason + ")");
        }

        /// <summary>
        /// Adds a failed check to the summary.
        /// </summary>
        /// <param name="description">The message that describes in a short manner the check that was failed.</param>
        /// <param name="members">The additional members that were involved in the check (both single values as tuples are supported).</param>
        internal void AddFailed(string description, params object[] members)
        {
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(members);

            string membersDescription = members.Length > 1
                ? $" ({string.Join(", ", members.Select(m =>
                {
                    return m switch
                    {
                        (string name, string value) => $"{name}={value}",
                        _ => m.ToString()
                    };
                }))}"
                : string.Empty;

            AddFailed(description + membersDescription);
        }

        private void AddFailed(string description)
        {
            _lines.Add("✗ " + description);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (_lines.Count == 1)
            {
                return _lines.Single();
            }

            return Environment.NewLine + string.Join(Environment.NewLine, _lines);
        }
    }
}
