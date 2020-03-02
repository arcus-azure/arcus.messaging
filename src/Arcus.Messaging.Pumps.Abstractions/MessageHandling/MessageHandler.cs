using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents an abstracted form of the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementation to handle with different type of generic message and message context types.
    /// </summary>
    internal class MessageHandler
    {
        private readonly Type _serviceType;
        private readonly Func<object> _createMessageHandlerImplementation;

        private MessageHandler(Type serviceType, Func<object> createMessageHandlerImplementation)
        {
            Guard.NotNull(serviceType, nameof(serviceType));
            Guard.NotNull(createMessageHandlerImplementation, nameof(createMessageHandlerImplementation));

            _serviceType = serviceType;
            _createMessageHandlerImplementation = createMessageHandlerImplementation;
        }

        /// <summary>
        /// Subtract all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementations from the given <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="serviceProvider">The provided registered services collection.</param>
        internal static IEnumerable<MessageHandler> SubtractFrom(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            object engine = serviceProvider.GetPropertyValue("Engine");
            object callSiteFactory = engine.GetPropertyValue("CallSiteFactory", BindingFlags.NonPublic | BindingFlags.Instance);

            var descriptorLookup = (System.Collections.IEnumerable) callSiteFactory.GetFieldValue("_descriptorLookup");

            var messageHandlers = new Collection<MessageHandler>();
            foreach (object lookup in descriptorLookup)
            {
                var serviceType = (Type) lookup.GetPropertyValue("Key");
                
                if ( serviceType?.Name.Contains("IMessageHandler") == true)
                {
                    object cacheItem = lookup.GetPropertyValue("Value");
                    var descriptors = (System.Collections.IEnumerable) cacheItem.GetFieldValue("_items");

                    foreach (object descriptor in descriptors)
                    {
                        var messageHandler = new MessageHandler(
                            serviceType, 
                            () => CreateMessageHandlerImplementation(descriptor, callSiteFactory, engine, serviceType, serviceProvider));
                    
                        messageHandlers.Add(messageHandler);
                    }
                }
            }

            return messageHandlers.AsEnumerable();
        }

        private static object CreateMessageHandlerImplementation(object descriptor, object callSiteFactory, object engine, Type serviceType, IServiceProvider serviceProvider)
        {
            object lifetime = descriptor.GetPropertyValue("Lifetime");
            object resultCache = CreateResultCache(lifetime, serviceType);
            object implementationType = descriptor.GetPropertyValue("ImplementationType");
            object callSiteChain = CreateCallSiteChain();

            callSiteChain.InvokeMethod("CheckCircularDependency", BindingFlags.Instance | BindingFlags.Public, serviceType);
            object callSite = callSiteFactory.InvokeMethod("CreateConstructorCallSite", resultCache, serviceType, implementationType, callSiteChain);

            callSiteFactory.GetFieldValue("_callSiteCache")
                           .SetIndexValue("Item", serviceType, callSite);

            return engine.InvokeMethod("RealizeService", callSite)
                         .InvokeMethod("Invoke", BindingFlags.Public | BindingFlags.Instance, serviceProvider);
        }

        private static object CreateResultCache(object lifetime, Type serviceType)
        {
            var resultCacheType = Type.GetType("Microsoft.Extensions.DependencyInjection.ServiceLookup.ResultCache, Microsoft.Extensions.DependencyInjection");
            if (resultCacheType is null)
            {
                throw new TypeNotFoundException(
                    "Cannot find a 'ResultCache' type in the 'Microsoft.Extensions.DependencyInjection' assembly");
            }
            
            return Activator.CreateInstance(resultCacheType, lifetime, serviceType, 0);
        }

        private static object CreateCallSiteChain()
        {
            var callSiteChainType = Type.GetType("Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteChain, Microsoft.Extensions.DependencyInjection");
            if (callSiteChainType is null)
            {
                throw new TypeNotFoundException(
                    "Cannot find a 'CallSiteChain' type in the 'Microsoft.Extensions.DependencyInjection' assembly");
            }

            return Activator.CreateInstance(callSiteChainType);
        }

        /// <summary>
        /// Tries to parse the given raw <paramref name="message"/> to the contract of the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
        /// </summary>
        /// <typeparam name="TMessageContext">The type of the message context the <see cref="IMessageHandler{TMessage,TMessageContext}"/> uses.</typeparam>
        /// <param name="message">The raw incoming message that will be tried to parse against the <see cref="IMessageHandler{TMessage,TMessageContext}"/>'s message contract.</param>
        /// <param name="result">The resulted parsed message when the <paramref name="message"/> conforms with the message handlers' contract.</param>
        /// <returns>
        ///     [true] if the <paramref name="message"/> conforms the <see cref="IMessageHandler{TMessage,TMessageContext}"/>'s contract; otherwise [false].
        /// </returns>
        internal bool TryParseToMessageFormat<TMessageContext>(string message, out object? result)
        {
            Guard.NotNullOrWhitespace(message, nameof(message), "Can't parse a blank raw message against a message handler's contract");

            Type messageType = _serviceType.GenericTypeArguments[0];
            Type messageContextType = _serviceType.GenericTypeArguments[1];

            if (typeof(TMessageContext) != messageContextType)
            {
                result = null;
                return false;
            }

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
                result = value;
                return true;
            }

            result = null;
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
        public async Task ProcessMessageAsync<TMessageContext>(
            object message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken) where TMessageContext : class
        {
            Guard.NotNull(message, nameof(message));
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(correlationInfo, nameof(correlationInfo));

            object messageHandlerImplementation = _createMessageHandlerImplementation();
            Type messageType = _serviceType.GenericTypeArguments[0];
            Type messageContextType = _serviceType.GenericTypeArguments[1];

            MethodInfo processMessageAsyncMethod = 
                messageHandlerImplementation.GetType().GetMethod(
                    "ProcessMessageAsync",
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    CallingConventions.Any,
                    new[] { messageType, messageContextType, typeof(MessageCorrelationInfo), typeof(CancellationToken) },
                    modifiers: null);

            const string methodName = "ProcessMessageAsync";
            var processMessageAsync = 
                (Task) messageHandlerImplementation.InvokeMethod(
                    methodName, BindingFlags.Instance | BindingFlags.Public, message, messageContext, correlationInfo, cancellationToken);

            if (processMessageAsync is null)
            {
                throw new InvalidOperationException(
                    $"The '{typeof(IMessageHandler<,>).Name}' implementation '{messageHandlerImplementation.GetType().Name}' returned 'null' while calling the '{methodName}' method");
            }

            await processMessageAsync;
        }
    }
}
