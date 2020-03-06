using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
    public class MessageHandler
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
        public static IEnumerable<MessageHandler> SubtractFrom(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            object engine = serviceProvider.GetRequiredPropertyValue("Engine");
            object callSiteFactory = engine.GetRequiredPropertyValue("CallSiteFactory", BindingFlags.NonPublic | BindingFlags.Instance);

            var descriptorLookup = callSiteFactory.GetRequiredFieldValue<IEnumerable>("_descriptorLookup");

            var messageHandlers = new Collection<MessageHandler>();
            foreach (object lookup in descriptorLookup)
            {
                var serviceType = lookup.GetRequiredPropertyValue<Type>("Key");
                if (serviceType.Name == "IMessageHandler`2" 
                    && serviceType.Namespace == typeof(IMessageHandler<>).Namespace)
                {
                    IEnumerable descriptors = GetServiceDescriptors(lookup);
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

        private static IEnumerable GetServiceDescriptors(object descriptorLookup)
        {
            object cacheItem = descriptorLookup.GetRequiredPropertyValue("Value");
            var descriptors = cacheItem.GetFieldValue<IEnumerable>("_items");
            if (descriptors is null)
            {
                object descriptor = cacheItem.GetFieldValue("_item");
                if (descriptor is null)
                {
                    return Enumerable.Empty<object>();
                }

                return new[] { descriptor };
            }

            return descriptors;
        }

        private static object CreateMessageHandlerImplementation(object descriptor, object callSiteFactory, object engine, Type serviceType, IServiceProvider serviceProvider)
        {
            object lifetime = descriptor.GetRequiredPropertyValue("Lifetime");
            object resultCache = CreateResultCache(lifetime, serviceType);
            object implementationType = descriptor.GetRequiredPropertyValue("ImplementationType");
            object callSiteChain = CreateCallSiteChain();

            callSiteChain.InvokeMethod("CheckCircularDependency", BindingFlags.Instance | BindingFlags.Public, serviceType);
            object callSite = callSiteFactory.InvokeMethod("CreateConstructorCallSite", resultCache, serviceType, implementationType, callSiteChain);

            callSiteFactory.GetRequiredFieldValue("_callSiteCache")
                           .SetIndexValue("Item", serviceType, callSite);

            object func = engine.InvokeMethod("RealizeService", callSite);
            if (func is null)
            {
                throw new ValueMissingException(
                    $"Invoking method 'RealizeService' on instance '{callSite.GetType().Name}' doesn't result in a required return value");
            }
            
            return func.InvokeMethod("Invoke", BindingFlags.Public | BindingFlags.Instance, serviceProvider);
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

            if (typeof(TMessageContext) != messageContextType && messageContextType != typeof(MessageContext))
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
