using System;
using Arcus.Messaging;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents the available options when registering an <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.
    /// </summary>
    /// <typeparam name="TMessage">The custom message type to handler.</typeparam>
    public class ServiceBusMessageHandlerOptions<TMessage>
    {
        internal Func<IServiceProvider, IMessageBodySerializer> MessageBodySerializerImplementationFactory { get; private set; }
        internal Func<TMessage, bool> MessageBodyFilter { get; private set; }
        internal Func<ServiceBusMessageContext, bool> MessageContextFilter { get; private set; }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serializer"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(IMessageBodySerializer serializer)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            return AddMessageBodySerializer(_ => serializer);
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(Func<IServiceProvider, IMessageBodySerializer> implementationFactory)
        {
            ArgumentNullException.ThrowIfNull(implementationFactory);
            MessageBodySerializerImplementationFactory = implementationFactory;

            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="bodyFilter"/> to only select a subset of messages, based on its body, that the registered message handler can handle.
        /// </summary>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodyFilter(Func<TMessage, bool> bodyFilter)
        {
            ArgumentNullException.ThrowIfNull(bodyFilter);
            MessageBodyFilter = bodyFilter;

            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="contextFilter"/> to only select a subset of messages, based on its context, that the registered message handler can handle.
        /// </summary>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageContextFilter(Func<ServiceBusMessageContext, bool> contextFilter)
        {
            ArgumentNullException.ThrowIfNull(contextFilter);
            MessageContextFilter = contextFilter;

            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="contextFilter"/> to only select a subset of messages, based on its context, that the registered message handler can handle.
        /// </summary>
#pragma warning disable S1133
        [Obsolete("Will be removed in v4.0, please use the other overload that accepts a ServiceBusMessageContext instead")]
#pragma warning restore S1133
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageContextFilter(Func<AzureServiceBusMessageContext, bool> contextFilter)
        {
            ArgumentNullException.ThrowIfNull(contextFilter);
            MessageContextFilter = contextFilter;

            return this;
        }
    }
}