using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
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
        internal Type MessageType { get; }

        /// <summary>
        /// Gets the type of the message context that this abstracted message handler can process.
        /// </summary>
        internal Type MessageContextType { get; }

        /// <summary>
        /// Subtract all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementations from the given <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="serviceProvider">The provided registered services collection.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the lifetime of the message handlers.</param>
        public static IEnumerable<MessageHandler> SubtractFrom(IServiceProvider serviceProvider, ILogger logger)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a collection of services to subtract the message handlers from");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write trace messages during the lifetime of the message handlers");

            object engine = 
                serviceProvider.GetType().GetProperty("Engine")?.GetValue(serviceProvider) 
                ?? serviceProvider.GetRequiredFieldValue("_engine");
            
            object callSiteFactory = engine.GetRequiredPropertyValue("CallSiteFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            var descriptors = callSiteFactory.GetRequiredFieldValue<IEnumerable>("_descriptors");

            var messageHandlers = new Collection<MessageHandler>();
            foreach (object descriptor in descriptors)
            {
                var serviceType = (Type) descriptor.GetRequiredPropertyValue("ServiceType");
                if (serviceType.Name == typeof(IMessageHandler<,>).Name 
                    && serviceType.Namespace == typeof(IMessageHandler<>).Namespace)
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
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        public bool CanProcessMessage<TMessageContext>(TMessageContext messageContext) where TMessageContext : MessageContext
        {
            Type expectedMessageContextType = _serviceType.GenericTypeArguments[1];
            Type actualMessageContextType = typeof(TMessageContext);

            if (actualMessageContextType == expectedMessageContextType)
            {
                _logger.LogInformation(
                    "Message context type '{ActualMessageContextType}' matches registered message handler's {MessageHandlerType} context type {ExpectedMessageContextType}",
                    actualMessageContextType.Name, _serviceType.Name, expectedMessageContextType.Name);

                if (_service.GetType().Name == typeof(MessageHandlerRegistration<,>).Name)
                {
                    _logger.LogTrace(
                        "Determining whether the message context predicate registered with the message handler {MessageHandlerType} holds...",
                         _serviceType.Name);

                    var canProcessMessage = (bool) _service.InvokeMethod(
                        "CanProcessMessage",
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        messageContext);

                    _logger.LogInformation(
                        "Message context predicate registered with the message handler {MessageHandlerType} resulted in {Result}, so {Action} process this message",
                        _serviceType.Name, canProcessMessage, canProcessMessage ? "can" : "can't");

                    return canProcessMessage;
                }

                // Message context type matches registration message context type; registered without predicate.
                return true;
            }

            _logger.LogInformation(
                "Message context type '{ActualMessageContextType}' doesn't matches registered message handler's {MessageHandlerType} context type {ExpectedMessageContextType}",
                actualMessageContextType.Name, _serviceType.Name, expectedMessageContextType.Name);

            // Message context type doesn't match registration message context type.
            return false;
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
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="TypeNotFoundException">Thrown when no processing method was found on the message handler.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler cannot process the message correctly.</exception>
        /// <exception cref="AmbiguousMatchException">Thrown when more than a single processing method was found on the message handler.</exception>
        public async Task ProcessMessageAsync<TMessageContext>(
            object message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken) where TMessageContext : class
        {
            Guard.NotNull(message, nameof(message), "Requires message content to deserialize and process the message");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires a message context to send to the message handler");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires correlation information to send to the message handler");

            Type messageHandlerType = GetMessageHandlerType();
            _logger.LogTrace(
                "Start processing '{MessageType}' message in message handler '{MessageHandlerType}'...", 
                message.GetType().Name, messageHandlerType.Name);

            const string methodName = "ProcessMessageAsync";
            try
            {
                var processMessageAsync =
                        (Task)_service.InvokeMethod(
                            methodName, BindingFlags.Instance | BindingFlags.Public, message, messageContext, correlationInfo, cancellationToken);

                if (processMessageAsync is null)
                {
                    throw new InvalidOperationException(
                        $"The '{typeof(IMessageHandler<,>).Name}' implementation '{messageHandlerType.Name}' returned 'null' while calling the '{methodName}' method");
                }

                await processMessageAsync;

                _logger.LogInformation(
                    "Message handler '{MessageHandlerType}' successfully processed '{MessageType}' message", messageHandlerType.Name, MessageType.Name);
            }
            catch (AmbiguousMatchException exception)
            {
                _logger.LogError(
                    "Ambiguous match found of '{MethodName}' methods in the '{MessageHandlerType}'. Make sure that only 1 matching '{MethodName}' was found on the '{MessageHandlerType}' message handler",
                    methodName, messageHandlerType.Name, methodName, messageHandlerType.Name);

                throw new AmbiguousMatchException(
                    $"Ambiguous match found of '{methodName}' methods in the '{messageHandlerType.Name}'. ", exception);
            }
        }
    }
}
