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

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents an abstracted form of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation to handle with different type of generic message and message context types.
    /// </summary>
    public class MessageHandler
    {
        private readonly object _service;
        private readonly Type _serviceType;

        private MessageHandler(Type serviceType, object service)
        {
            Guard.NotNull(serviceType, nameof(serviceType));
            Guard.NotNull(service, nameof(service));
            Guard.For<ArgumentException>(
                () => serviceType.GenericTypeArguments.Length != 2, 
                $"Message handler type '{serviceType.Name}' has not the expected 2 generic type arguments");

            _serviceType = serviceType;
            _service = service;

            MessageType = _serviceType.GenericTypeArguments[0];
        }

        /// <summary>
        /// Gets the type of the message that this abstracted message handler can process.
        /// </summary>
        internal Type MessageType { get; }

        /// <summary>
        /// Subtract all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementations from the given <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="serviceProvider">The provided registered services collection.</param>
        public static IEnumerable<MessageHandler> SubtractFrom(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

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
                        var messageHandler = new MessageHandler(serviceType, service);
                        messageHandlers.Add(messageHandler);
                    }
                }
            }

            return messageHandlers.AsEnumerable();
        }


        /// <summary>
        /// Determines if the given <typeparamref name="TMessageContext"/> matches the generic parameter of this message handler.
        /// </summary>
        /// <typeparam name="TMessageContext">The type of the message context.</typeparam>
        public bool CanProcessMessage<TMessageContext>(TMessageContext messageContext) where TMessageContext : MessageContext
        {
            Type messageContextType = _serviceType.GenericTypeArguments[1];
            bool matchesType = typeof(TMessageContext) == messageContextType || messageContextType == typeof(MessageContext);
            if (!matchesType)
            {
                return false;
            }

            if (_service.GetType().Name == typeof(MessageHandlerRegistration<,>).Name)
            {
                return (bool) _service.InvokeMethod(
                    "CanProcessMessage", BindingFlags.Instance | BindingFlags.NonPublic, messageContext);
            }

            return true;
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
        public async Task ProcessMessageAsync<TMessageContext>(
            object message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken) where TMessageContext : class
        {
            Guard.NotNull(message, nameof(message));
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(correlationInfo, nameof(correlationInfo));

            const string methodName = "ProcessMessageAsync";
            var processMessageAsync = 
                (Task) _service.InvokeMethod(
                    methodName, BindingFlags.Instance | BindingFlags.Public, message, messageContext, correlationInfo, cancellationToken);

            if (processMessageAsync is null)
            {
                throw new InvalidOperationException(
                    $"The '{typeof(IMessageHandler<,>).Name}' implementation '{_service.GetType().Name}' returned 'null' while calling the '{methodName}' method");
            }

            await processMessageAsync;
        }
    }
}
