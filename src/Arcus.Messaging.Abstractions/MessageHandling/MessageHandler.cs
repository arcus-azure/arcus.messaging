using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents an abstracted form of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation to handle with different type of generic message and message context types.
    /// </summary>
    public class MessageHandler
    {
        private readonly object _service;
        private readonly Type _serviceType;
        private readonly ILogger _logger;

        private MessageHandler(Type serviceType, object service, ILogger logger)
        {
            Guard.NotNull(serviceType, nameof(serviceType), "Requires a message handler type");
            Guard.NotNull(service, nameof(service), "Requires a message handler instance");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance");
            Guard.For<ArgumentException>(
                () => serviceType.GenericTypeArguments.Length != 2, 
                $"Message handler type '{serviceType.Name}' has not the expected 2 generic type arguments");

            _service = service;
            _serviceType = serviceType;
            _logger = logger;

            MessageType = _serviceType.GenericTypeArguments[0];
            MessageContextType = _serviceType.GenericTypeArguments[1];
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

            Type[] serviceTypes = GetServiceRegistrationTypes(serviceProvider, logger);

            var messageHandlers = new Collection<MessageHandler>();
            foreach (Type serviceType in serviceTypes)
            {
                if (serviceType.Name == typeof(IMessageHandler<,>).Name 
                    && serviceType.Namespace == typeof(IMessageHandler<>).Namespace
                    && messageHandlers.All(handler => handler._serviceType != serviceType))
                {
                    IEnumerable<object> services = serviceProvider.GetServices(serviceType);
                    foreach (object service in services)
                    {
                        var messageHandler = new MessageHandler(serviceType, service, logger);
                        messageHandlers.Add(messageHandler);
                    }
                }
            }

            return messageHandlers.AsEnumerable();
        }

        private static Type[] GetServiceRegistrationTypes(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                Type[] serviceTypes = 
                    GetServiceRegistrationTypesFromMicrosoftExtensions(serviceProvider, logger)
                        .Concat(GetServiceRegistrationTypesFromWebJobs(serviceProvider, logger))
                        .ToArray();

                if (serviceTypes.Length <= 0)
                {
                    logger.LogWarning("No message handlers registrations were found in the service provider");
                }
                
                return serviceTypes;
            }
            catch (Exception exception) when (exception is TypeNotFoundException || exception is InvalidCastException)
            {
                const string message = "The registered message handlers cannot be retrieved from the service provider because the current version of the dependency injection package doesn't match the expected package used in the Arcus messaging";
                logger.LogCritical(exception, message);
                throw new NotSupportedException(message, exception);
            }
        }

        private static IEnumerable<Type> GetServiceRegistrationTypesFromMicrosoftExtensions(IServiceProvider serviceProvider, ILogger logger)
        {
            object engine = 
                serviceProvider.GetType().GetProperty("Engine")?.GetValue(serviceProvider) 
                ?? serviceProvider.GetType().GetField("_engine", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(serviceProvider);

            if (engine is null)
            {
                logger.LogTrace("No message handling registrations using the Microsoft.Extensions.* package, expected if you're within Web Jobs/Azure Functions");
                return Enumerable.Empty<Type>();
            }

            object callSiteFactory = engine.GetRequiredPropertyValue("CallSiteFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            var descriptors = callSiteFactory.GetRequiredFieldValue<IEnumerable>("_descriptors");

            var serviceTypes = new Collection<Type>();
            foreach (object descriptor in descriptors)
            {
                var serviceType = descriptor.GetRequiredPropertyValue<Type>("ServiceType");
                serviceTypes.Add(serviceType);
            }

            return serviceTypes;

        }

        private static IEnumerable<Type> GetServiceRegistrationTypesFromWebJobs(IServiceProvider serviceProvider, ILogger logger)
        {
            object container = serviceProvider.GetFieldValue("_container");
            if (container is null)
            {
                logger.LogTrace("No message handling registrations using the Web Jobs/Azure Function package, expected if you're within SDK worker");
                return Enumerable.Empty<Type>();
            }
            
            var descriptors = (IEnumerable) container.InvokeMethod("GetServiceRegistrations", BindingFlags.Public | BindingFlags.Instance);

            var serviceTypes = new Collection<Type>();
            foreach (object descriptor in descriptors)
            {
                var serviceType = descriptor.GetRequiredFieldValue<Type>("ServiceType");
                serviceTypes.Add(serviceType);
            }

            return serviceTypes;
        }

        /// <summary>
        /// Gets the concrete class type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        /// <exception cref="TypeNotFoundException">Thrown when there's a problem with finding the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</exception>
        /// <exception cref="ValueMissingException">Thrown when there's no value for the <see cref="IMessageHandler{TMessage,TMessageContext}"/> type value.</exception>
        public object GetMessageHandlerInstance()
        {
            if (_service.GetType().Name == typeof(MessageHandlerRegistration<,>).Name)
            {
                object messageHandlerType = _service.GetRequiredPropertyValue("Service", BindingFlags.Instance | BindingFlags.NonPublic);
                return messageHandlerType;
            }

            return _service;
        }

        /// <summary>
        /// Gets the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instance.
        /// </summary>
        /// <exception cref="TypeNotFoundException">Thrown when there's a problem with finding the type of the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.</exception>
        /// <exception cref="ValueMissingException">Thrown when there's no value for the <see cref="IMessageHandler{TMessage,TMessageContext}"/> type value.</exception>
        public Type GetMessageHandlerType()
        {
            object messageHandlerInstance = GetMessageHandlerInstance();
            return messageHandlerInstance.GetType();
        }

        /// <summary>
        /// Determines if the given <typeparamref name="TMessageContext"/> matches the generic parameter of this message handler.
        /// </summary>
        /// <param name="messageContext">The messaging context information that holds information about the currently processing message.</param>
        /// <returns>
        ///     [true] if the registered <typeparamref name="TMessageContext"/> predicate holds; [false] otherwise.
        /// </returns>
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        /// <param name="messageContext">The context in which the incoming message is processed.</param>
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

            Type expectedMessageContextType = _serviceType.GenericTypeArguments[1];
            Type actualMessageContextType = typeof(TMessageContext);

            if (actualMessageContextType == expectedMessageContextType)
            {
                _logger.LogTrace("Message context type '{ActualMessageContextType}' matches registered message handler's {MessageHandlerType} context type {ExpectedMessageContextType}", 
                    actualMessageContextType.Name, _serviceType.Name, expectedMessageContextType.Name);

                if (_service.GetType().Name == typeof(MessageHandlerRegistration<,>).Name)
                {
                    bool canProcessMessageWithinMessageContext = CanProcessMessageWithinMessageContext(messageContext);
                    return canProcessMessageWithinMessageContext;
                }

                // Message context type matches registration message context type; registered without predicate.
                return true;
            }

            _logger.LogTrace("Message context type '{ActualMessageContextType}' doesn't matches registered message handler's {MessageHandlerType} context type {ExpectedMessageContextType}", 
                actualMessageContextType.Name, _serviceType.Name, expectedMessageContextType.Name);

            // Message context type doesn't match registration message context type.
            return false;
        }

        private bool CanProcessMessageWithinMessageContext<TMessageContext>(TMessageContext messageContext)
            where TMessageContext : MessageContext
        {
            _logger.LogTrace("Determining whether the message context predicate registered with the message handler {MessageHandlerType} holds...", _serviceType.Name);

            var canProcessMessage = 
                (bool) _service.InvokeMethod("CanProcessMessageWithinMessageContext", BindingFlags.Instance | BindingFlags.NonPublic, messageContext);

            _logger.LogTrace("Message context predicate registered with the message handler {MessageHandlerType} resulted in {Result}, so {Action} process this message", 
                _serviceType.Name, canProcessMessage, canProcessMessage ? "can" : "can't");
            
            return canProcessMessage;
        }

        /// <summary>
        /// Determines if the registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> can process the incoming deserialized message based on the consumer-provided message predicate.
        /// </summary>
        /// <param name="message">The incoming deserialized message body.</param>
        public bool CanProcessMessageBasedOnMessage(object? message)
        {
            if (_service.GetType().Name == typeof(MessageHandlerRegistration<,>).Name)
            {
                try
                {
                    _logger.LogTrace("Determining whether the message context predicate registered with the message handler {MessageHandlerType} holds...", _serviceType.Name);

                    var canProcessMessage = 
                        (bool) _service.InvokeMethod("CanProcessMessageBasedOnMessage", BindingFlags.Instance | BindingFlags.NonPublic, message);

                    _logger.LogTrace("Message predicate registered with the message handler {MessageHandlerType} resulted in {Result}, so {Action} process this message", 
                        _serviceType.Name, canProcessMessage, canProcessMessage ? "can" : "can't");
            
                    return canProcessMessage;   
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Message predicate faulted during execution");
                    return false;
                }
            }

            return true;
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
            if (_service.GetType().Name == typeof(MessageHandlerRegistration<,>).Name)
            {
                _logger.LogTrace("Custom {MessageBodySerializerType} found on the registered message handler, start custom deserialization...", nameof(IMessageBodySerializer));

                var serializer = _service.GetPropertyValue<IMessageBodySerializer>("Serializer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (serializer != null)
                {
                    Task<MessageResult> deserializeMessageAsync = serializer.DeserializeMessageAsync(message);
                    if (deserializeMessageAsync != null)
                    {
                        MessageResult result = await deserializeMessageAsync;
                        if (result != null)
                        {
                            if (result.IsSuccess)
                            {
                                Type deserializedMessageType = result.DeserializedMessage.GetType();
                                if (deserializedMessageType == MessageType
                                    || deserializedMessageType.IsSubclassOf(MessageType))
                                {
                                    return result;
                                }

                                _logger.LogTrace("Incoming message '{DeserializedMessageType}' was successfully custom deserialized but can't be processed by message handler because the handler expects message type '{MessageHandlerMessageType}'; fallback to default deserialization", deserializedMessageType.Name, MessageType.Name);
                                return MessageResult.Failure("Custom message deserialization failed because it didn't match the expected message handler's message type");
                            }

                            _logger.LogTrace("Custom {MessageBodySerializerType} message deserialization failed: {ErrorMessage} {Exception}", nameof(IMessageBodySerializer), result.ErrorMessage, result.Exception);
                            return MessageResult.Failure("Custom message deserialization failed due to an exception");
                        }
                    }

                    _logger.LogTrace("Invalid {MessageBodySerializerType} message deserialization was configured on the registered message handler, custom deserialization returned 'null'", nameof(IMessageBodySerializer));
                    return MessageResult.Failure("Invalid custom deserialization was configured on the registered message handler");
                }
            }

            _logger.LogTrace("No {MessageBodySerializerType} was found on the registered message handler, so no custom deserialization is available", nameof(IMessageBodySerializer));
            return MessageResult.Failure("No custom deserialization was found on the registered message handler");
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
        /// <exception cref="TypeNotFoundException">Thrown when no processing method was found on the message handler.</exception>
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

            Type messageHandlerType = GetMessageHandlerType();
            Type messageType = message.GetType();
            _logger.LogTrace("Start processing '{MessageType}' message in message handler '{MessageHandlerType}'...", messageType, messageHandlerType.Name);

            const string methodName = nameof(IMessageHandler<object, MessageContext>.ProcessMessageAsync);
            try
            {
                var processMessageAsync =
                    (Task) _service.InvokeMethod(
                        methodName, BindingFlags.Instance | BindingFlags.Public, message, messageContext, correlationInfo, cancellationToken);

                if (processMessageAsync is null)
                {
                    throw new InvalidOperationException(
                        $"The '{typeof(IMessageHandler<,>).Name}' implementation '{messageHandlerType.Name}' returned 'null' while calling the '{methodName}' method");
                }

                await processMessageAsync;
                _logger.LogTrace("Message handler '{MessageHandlerType}' successfully processed '{MessageType}' message", messageHandlerType.Name, messageType.Name);

                return true;
            }
            catch (AmbiguousMatchException exception)
            {
                _logger.LogError(exception,
                    "Ambiguous match found of '{MethodName}' methods in the '{MessageHandlerType}'. Make sure that only 1 matching '{MethodName}' was found on the '{MessageHandlerType}' message handler",
                    methodName, messageHandlerType.Name, methodName, messageHandlerType.Name);

                return false;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Message handler '{MessageHandlerType}' failed to process '{MessageType}' due to a thrown exception", messageHandlerType.Name, messageType.Name);
                return false;
            }
        }
    }
}
