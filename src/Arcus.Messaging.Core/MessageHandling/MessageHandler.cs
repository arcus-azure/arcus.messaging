using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessMessageAsync = System.Func<object, Arcus.Messaging.Abstractions.MessageContext, Arcus.Messaging.MessageCorrelationInfo, System.Threading.CancellationToken, System.Threading.Tasks.Task<Arcus.Messaging.Abstractions.MessageHandling.MessageProcessingResult>>;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the available options when registering an <see cref="IMessageHandler{TMessage, TMessageContext}"/>.
    /// </summary>
    /// <typeparam name="TMessage">The custom message type of the message handler.</typeparam>
    /// <typeparam name="TMessageContext">The custom message context type of the message handler.</typeparam>
    public abstract class MessageHandlerOptions<TMessage, TMessageContext> where TMessageContext : MessageContext
    {
        private readonly Collection<Func<TMessage, bool>> _messageBodyFilters = [];
        private readonly Collection<Func<TMessageContext, bool>> _messageContextFilters = [];

        internal Func<IServiceProvider, IMessageBodyDeserializer> MessageBodyDeserializerImplementationFactory { get; private set; }
        internal Func<TMessage, bool> MessageBodyFilter => _messageBodyFilters.Count is 0 ? null : msg => _messageBodyFilters.All(filter => filter(msg));
        internal Func<TMessageContext, bool> MessageContextFilter => _messageContextFilters.Count is 0 ? null : ctx => _messageContextFilters.All(filter => filter(ctx));

        /// <summary>
        /// Adds a custom <paramref name="contextFilter"/> to only select a subset of messages, based on its context, that the registered message handler can handle.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="contextFilter"/> is <c>null</c>.</exception>
        protected void AddContextFilter(Func<TMessageContext, bool> contextFilter)
        {
            ArgumentNullException.ThrowIfNull(contextFilter);
            _messageContextFilters.Add(contextFilter);
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming message body.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        protected void UseBodyDeserializer(Func<IServiceProvider, IMessageBodyDeserializer> implementationFactory)
        {
            ArgumentNullException.ThrowIfNull(implementationFactory);
            MessageBodyDeserializerImplementationFactory = implementationFactory;
        }

        /// <summary>
        /// Adds a custom <paramref name="bodyFilter"/> to only select a subset of messages, based on its body, that the registered message handler can handle.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="bodyFilter"/> is <c>null</c>.</exception>
        protected void AddBodyFilter(Func<TMessage, bool> bodyFilter)
        {
            ArgumentNullException.ThrowIfNull(bodyFilter);
            _messageBodyFilters.Add(bodyFilter);
        }
    }

    /// <summary>
    /// Represents an abstracted form of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation to handle with different type of generic message and message context types.
    /// </summary>
    public class MessageHandler
    {
        private readonly object _messageHandlerInstance;
        private readonly ProcessMessageAsync _messageHandlerImplementation;
        private readonly IMessageBodyDeserializer _messageBodyDeserializer;
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
            IMessageBodyDeserializer messageBodyDeserializer,
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
            _messageBodyDeserializer = messageBodyDeserializer;
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
        /// Creates a general <see cref="MessageHandler"/> instance from the <typeparamref name="TMessageHandler"/> instance.
        /// </summary>
        /// <typeparam name="TMessage">The type of message the <typeparamref name="TMessageHandler"/> processes.</typeparam>
        /// <typeparam name="TMessageContext">The type of context the <typeparamref name="TMessageHandler"/> processes.</typeparam>
        /// <typeparam name="TMessageHandler">The type of the message handler to process the <typeparamref name="TMessage"/>.</typeparam>
        /// <param name="implementationFactory">The factory function to create a user-defined message handler instance.</param>
        /// <param name="options">The optional set of options to configure the message handler registration.</param>
        /// <param name="serviceProvider">The current service provider instance to resolve application services.</param>
        /// <param name="jobId">The job ID to link this message handler to a registered message pump.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <typeparamref name="TMessageHandler"/> or the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public static MessageHandler Create<TMessage, TMessageContext, TMessageHandler>(
            Func<IServiceProvider, TMessageHandler> implementationFactory,
            MessageHandlerOptions<TMessage, TMessageContext> options,
            IServiceProvider serviceProvider,
            string jobId)
            where TMessageContext : MessageContext
            where TMessageHandler : IMessageHandler<TMessage, TMessageContext>
        {
            ArgumentNullException.ThrowIfNull(implementationFactory);
            ArgumentNullException.ThrowIfNull(serviceProvider);

            var messageHandler = implementationFactory(serviceProvider);
            ProcessMessageAsync processMessageAsync = DetermineMessageImplementation(messageHandler);
            ILogger logger = serviceProvider.GetService<ILogger<TMessageHandler>>() ?? NullLogger<TMessageHandler>.Instance;

            return new MessageHandler(
                messageHandlerInstance: messageHandler,
                messageHandlerImplementation: processMessageAsync,
                messageType: typeof(TMessage),
                messageContextType: typeof(TMessageContext),
                messageContextFilter: DetermineMessageContextFilter(options.MessageContextFilter, jobId),
                messageBodyFilter: DetermineMessageBodyFilter(options.MessageBodyFilter),
                messageBodyDeserializer: options.MessageBodyDeserializerImplementationFactory?.Invoke(serviceProvider),
                logger: logger);
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
        [Obsolete("Will be removed in v4.0 in favor of the new factory method with message handler options", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
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
                messageBodyDeserializer: messageBodySerializer is null ? null : new DeprecatedMessageBodyDeserializerAdapter(messageBodySerializer),
                logger: logger);
        }

        [Obsolete("Will be removed in v4.0", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        private sealed class DeprecatedMessageBodyDeserializerAdapter(IMessageBodySerializer deprecated) : IMessageBodyDeserializer
        {
            public async Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody)
            {
                try
                {
                    string messageBodyTxt = messageBody.IsEmpty ? string.Empty : messageBody.ToString();
                    MessageResult deprecatedResult = await deprecated.DeserializeMessageAsync(messageBodyTxt);

                    if (deprecatedResult.IsSuccess)
                    {
                        return MessageBodyResult.Success(deprecatedResult.DeserializedMessage);
                    }

                    return deprecatedResult.Exception is not null
                        ? MessageBodyResult.Failure(deprecatedResult.ErrorMessage, deprecatedResult.Exception)
                        : MessageBodyResult.Failure(deprecatedResult.ErrorMessage);
                }
                catch (Exception deserializationException)
                {
                    return MessageBodyResult.Failure("deserialization of message body was interrupted by an unexpected exception", deserializationException);
                }
            }
        }

        private static ProcessMessageAsync DetermineMessageImplementation<TMessage, TMessageContext>(IMessageHandler<TMessage, TMessageContext> messageHandler)
            where TMessageContext : MessageContext
        {
            return async (rawMessage, generalMessageContext, correlationInfo, cancellationToken) =>
            {
                var message = (TMessage) rawMessage;
                var messageContext = (TMessageContext) generalMessageContext;

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
                    summary.AddFailed("custom body filter failed", check => check.AddMember("requires type", typeof(TMessage).Name));
                    return false;
                }

                bool matches = messageBodyFilter(message);
                if (matches)
                {
                    summary.AddPassed("custom body filter passed", check => check.AddMember("against type", typeof(TMessage).Name));
                    return true;
                }

                summary.AddFailed("custom body filter failed", check => check.AddReason("returns 'false'"));
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
                    summary.AddFailed("custom context filter failed", check => check.AddMember("requires job ID", rawContext.JobId));
                    return false;
                }

                if (messageContextFilter is null)
                {
                    return true;
                }

                if (rawContext is not TMessageContext messageContext)
                {
                    summary.AddFailed("custom context filter failed", check => check.AddMember("requires type", typeof(TMessageContext).Name));
                    return false;
                }

                bool matches = messageContextFilter(messageContext);
                if (matches)
                {
                    summary.AddPassed("custom context filter passed", check => check.AddMember("against type", typeof(TMessageContext).Name));
                    return true;
                }

                summary.AddFailed("custom context filter failed", check => check.AddReason("returns 'false'"));
                return false;
            };
        }

        /// <summary>
        /// Gets the concrete class type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        [Obsolete("Will be removed in v4.0 in favor of centralizing message handler matching in message router", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public object GetMessageHandlerInstance()
        {
            return _messageHandlerInstance;
        }

        /// <summary>
        /// Gets the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        [Obsolete("Will be removed in v4.0 in favor of centralizing message handler matching in message router", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public Type GetMessageHandlerType()
        {
            return MessageHandlerType;
        }

        private bool MatchesMessageContext<TMessageContext>(TMessageContext context, MessageHandlerSummary summary)
            where TMessageContext : MessageContext
        {
            ArgumentNullException.ThrowIfNull(context);

            if (typeof(TMessageContext) != MessageContextType && !typeof(TMessageContext).IsSubclassOf(MessageContextType))
            {
                summary.AddFailed($"requires message context type {MessageContextType.Name}");
                return false;
            }

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
        [Obsolete("Will be removed in v4.0 in favor of centralizing message handler matching in message router", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public bool CanProcessMessageBasedOnContext<TMessageContext>(TMessageContext messageContext)
            where TMessageContext : MessageContext
        {
            return MatchesMessageContext(messageContext, new MessageHandlerSummary());
        }

        private bool MatchesMessageBody(object message, MessageHandlerSummary summary)
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
        [Obsolete("Will be removed in v4.0 in favor of centralizing message handler matching in message router", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public bool CanProcessMessageBasedOnMessage(object message)
        {
            return MatchesMessageBody(message, new MessageHandlerSummary());
        }

        private async Task<MessageBodyResult> TryCustomDeserializeMessageAsync(BinaryData messageBody, MessageHandlerSummary summary)
        {
            if (_messageBodyDeserializer is null)
            {
                return MessageBodyResult.Failure("n/a");
            }

            Type serializerType = _messageBodyDeserializer.GetType();
            MessageBodyResult result = null;
            try
            {
                Task<MessageBodyResult> deserializeMessageAsync = _messageBodyDeserializer.DeserializeMessageAsync(messageBody);

                if (deserializeMessageAsync is null)
                {
                    summary.AddFailed("custom body parsing failed",
                        check => check.AddMember("using type", serializerType.Name)
                                      .AddReason("returns 'null'"));

                    return MessageBodyResult.Failure("n/a");
                }

                result = await deserializeMessageAsync;
                if (result is null)
                {
                    summary.AddFailed("custom body parsing failed",
                        check => check.AddMember("using type", serializerType.Name)
                                      .AddReason("returns 'null'"));

                    return MessageBodyResult.Failure("n/a");
                }
            }
            catch (Exception exception)
            {
                summary.AddFailed(exception, "custom body parsing failed", check => check.AddMember("using type", serializerType.Name));
                return MessageBodyResult.Failure("n/a");
            }

            if (result.IsSuccess)
            {
                Type deserializedMessageType = result.DeserializedBody.GetType();
                if (deserializedMessageType == MessageType || deserializedMessageType.IsSubclassOf(MessageType))
                {
                    summary.AddPassed("custom body parsing passed", check => check.AddMember("using type", serializerType.Name));
                    return result;
                }

                summary.AddFailed("custom body parsing failed",
                    check => check.AddMember("using deserializer type", serializerType.Name)
                                  .AddReason($"requires message type={MessageType.Name}, got type={deserializedMessageType.Name}"));

                return MessageBodyResult.Failure("n/a");
            }

            if (result.FailureCause is not null)
            {
                summary.AddFailed(result.FailureCause, "custom body parsing failed", check => check.AddReason(result.FailureMessage));
            }
            else
            {
                summary.AddFailed("custom body parsing failed", check => check.AddReason(result.FailureMessage));
            }

            return MessageBodyResult.Failure("n/a");
        }

        /// <summary>
        /// Tries to custom deserialize the incoming <paramref name="message"/> via a optional additional <see cref="IMessageBodySerializer"/>
        /// that was provided with the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
        /// </summary>
        /// <param name="message">The incoming message to deserialize.</param>
        /// <returns>
        ///     A <see cref="MessageResult"/> instance that either represents a successful or faulted deserialization of the incoming <paramref name="message"/>.
        /// </returns>
        [Obsolete("Will be removed in v4.0 in favor of centralizing message handler matching in message router", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public async Task<MessageResult> TryCustomDeserializeMessageAsync(string message)
        {
            if (_messageBodyDeserializer is null)
            {
                return MessageResult.Failure("No custom deserialization was found on the registered message handler");
            }

            Task<MessageBodyResult> deserializeMessageAsync = _messageBodyDeserializer.DeserializeMessageAsync(BinaryData.FromString(message));
            if (deserializeMessageAsync is null)
            {
                _logger.LogTrace("Invalid {MessageBodySerializerType} message deserialization was configured on the registered message handler, custom deserialization returned 'null'", nameof(IMessageBodySerializer));
                return MessageResult.Failure("Invalid custom deserialization was configured on the registered message handler");
            }

            MessageBodyResult result = await deserializeMessageAsync;
            if (result is null)
            {
                _logger.LogTrace("No {MessageBodySerializerType} was found on the registered message handler, so no custom deserialization is available", nameof(IMessageBodySerializer));
                return MessageResult.Failure("No custom deserialization was found on the registered message handler");
            }

            if (result.IsSuccess)
            {
                Type deserializedMessageType = result.DeserializedBody.GetType();
                if (deserializedMessageType == MessageType || deserializedMessageType.IsSubclassOf(MessageType))
                {
                    return MessageResult.Success(result.DeserializedBody);
                }

                _logger.LogTrace("Incoming message '{DeserializedMessageType}' was successfully custom deserialized but can't be processed by message handler because the handler expects message type '{MessageHandlerMessageType}'; fallback to default deserialization", deserializedMessageType.Name, MessageType.Name);
                return MessageResult.Failure("Custom message deserialization failed because it didn't match the expected message handler's message type");
            }

            if (result.FailureCause != null)
            {
                _logger.LogError(result.FailureCause, "Custom {MessageBodySerializerType} message deserialization failed: {ErrorMessage}", nameof(IMessageBodySerializer), result.FailureMessage);
            }
            else
            {
                _logger.LogError("Custom {MessageBodySerializerType} message deserialization failed: {ErrorMessage}", nameof(IMessageBodySerializer), result.FailureMessage);
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
        [Obsolete("Will be removed in v4.0 in favor of centralizing message handler matching in message router", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
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
        /// Process the given <paramref name="messageBody"/> in the current <see cref="IMessageHandler{TMessage,TMessageContext}"/> representation.
        /// </summary>
        /// <typeparam name="TMessageContext">The type of the message context used in the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</typeparam>
        /// <param name="messageBody">The parsed message to be processed by the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</param>
        /// <param name="context">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">
        ///     The information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers.
        /// </param>
        /// <param name="options">The options configured on the message router related to deserializing a message.</param>
        /// <param name="cancellation">The token to cancel the message processing in the handler.</param>
        /// <returns>
        ///     [true] if the message handler was able to successfully process the message; [false] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageBody"/>, <paramref name="context"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler cannot process the message correctly.</exception>
        internal async Task<MessageProcessingResult> TryProcessMessageAsync<TMessageContext>(
            BinaryData messageBody,
            TMessageContext context,
            MessageCorrelationInfo correlationInfo,
            MessageDeserializationOptions options,
            CancellationToken cancellation)
            where TMessageContext : MessageContext
        {
            ArgumentNullException.ThrowIfNull(messageBody);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(correlationInfo);
            ArgumentNullException.ThrowIfNull(options);

            using var _ = _logger.BeginScope(new Dictionary<string, object> { ["JobId"] = context.JobId });
            var summary = new MessageHandlerSummary();

            if (!MatchesMessageContext(context, summary))
            {
                return MatchedHandlerSkipped();
            }

            MessageBodyResult bodyResult = await DeserializeMessageAsync(messageBody, context, options, summary);
            if (!bodyResult.IsSuccess)
            {
                return MatchedHandlerSkipped();
            }

            if (!MatchesMessageBody(bodyResult.DeserializedBody, summary))
            {
                return MatchedHandlerSkipped();
            }

            Type messageType = bodyResult.DeserializedBody.GetType();

            MessageProcessingResult processResult = await TryProcessMessageAsync(bodyResult.DeserializedBody, context, correlationInfo, cancellation);
            if (!processResult.IsSuccessful)
            {
                return MatchedHandlerFailed(processResult.ProcessingException, processResult.ErrorMessage);
            }

            _logger.LogMessageProcessedByHandler(messageType, context.MessageId, MessageHandlerType, summary);
            return MessageProcessingResult.Success(context.MessageId);

            MessageProcessingResult MatchedHandlerSkipped()
            {
                _logger.LogMessageSkippedByHandler(context.MessageId, MessageHandlerType, summary);
                return MessageProcessingResult.Failure(context.MessageId, MessageProcessingError.MatchedHandlerFailed, "n/a");
            }

            MessageProcessingResult MatchedHandlerFailed(Exception exception, string errorMessage)
            {
                summary.AddFailed(exception, "message processing failed", check => check.AddReason(errorMessage));

                _logger.LogMessageFailedInHandler(messageType, context.MessageId, MessageHandlerType, summary);
                return MessageProcessingResult.Failure(context.MessageId, MessageProcessingError.ProcessingInterrupted, "n/a");
            }
        }

        internal async Task<MessageBodyResult> DeserializeMessageAsync(
            BinaryData messageBody,
            MessageContext context,
            MessageDeserializationOptions options,
            MessageHandlerSummary summary)
        {
            MessageBodyResult result = await TryCustomDeserializeMessageAsync(messageBody, summary);
            if (result.IsSuccess)
            {
                return result;
            }

            Encoding encoding = DetermineEncoding(context);
            string json = messageBody.IsEmpty ? string.Empty : encoding.GetString(messageBody.ToArray());

            try
            {
                object deserializedByType = JsonSerializer.Deserialize(json, MessageType, options.JsonOptions);
                if (deserializedByType != null)
                {
                    summary.AddPassed("default JSON body parsing passed", check => check.AddMember("additional members", options.AdditionalMembers.ToString()));
                    return MessageBodyResult.Success(deserializedByType);
                }

                summary.AddFailed("default JSON body parsing failed", check => check.AddReason("returns 'null'"));
                return MessageBodyResult.Failure("n/a");
            }
            catch (JsonException exception)
            {
                summary.AddFailed(exception, "default JSON body parsing failed");
                return MessageBodyResult.Failure("n/a", exception);
            }
            catch (NotSupportedException exception)
            {
                summary.AddFailed(exception, "default JSON body parsing failed", check => check.AddReason("unsupported type"));
                return MessageBodyResult.Failure("n/a", exception);
            }
        }

        private static Encoding DetermineEncoding(MessageContext context)
        {
            Encoding fallbackEncoding = Encoding.UTF8;

            if (context.Properties.TryGetValue(PropertyNames.Encoding, out object encodingNameObj)
                && encodingNameObj is string encodingName
                && !string.IsNullOrWhiteSpace(encodingName))
            {
                EncodingInfo foundEncoding =
                    Encoding.GetEncodings()
                            .FirstOrDefault(e => e.Name.Equals(encodingName, StringComparison.OrdinalIgnoreCase));

                return foundEncoding?.GetEncoding() ?? fallbackEncoding;
            }

            return fallbackEncoding;
        }

        private async Task<MessageProcessingResult> TryProcessMessageAsync<TMessageContext>(
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
