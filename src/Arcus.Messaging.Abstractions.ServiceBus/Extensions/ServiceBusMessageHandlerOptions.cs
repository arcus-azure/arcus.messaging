using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly Collection<Func<TMessage, bool>> _messageBodyFilters = [];
        private readonly Collection<Func<AzureServiceBusMessageContext, bool>> _messageContextFilters = [];

        internal Func<IServiceProvider, IMessageBodySerializer> MessageBodySerializerImplementationFactory { get; private set; }
        internal Func<TMessage, bool> MessageBodyFilter => _messageBodyFilters.Count is 0 ? null : msg => _messageBodyFilters.All(filter => filter(msg));
        internal Func<AzureServiceBusMessageContext, bool> MessageContextFilter => _messageContextFilters.Count is 0 ? null : ctx => _messageContextFilters.All(filter => filter(ctx));

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <typeparam name="TSerializer">The custom <see cref="IMessageBodySerializer"/> type load from the application services.</typeparam>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodySerializer<TSerializer>()
            where TSerializer : IMessageBodySerializer
        {
            return UseMessageBodySerializer(serviceProvider => serviceProvider.GetRequiredService<TSerializer>());
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serializer"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodySerializer(IMessageBodySerializer serializer)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            return UseMessageBodySerializer(_ => serializer);
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodySerializer(Func<IServiceProvider, IMessageBodySerializer> implementationFactory)
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
            _messageBodyFilters.Add(bodyFilter);

            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="contextFilter"/> to only select a subset of messages, based on its context, that the registered message handler can handle.
        /// </summary>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageContextFilter(Func<AzureServiceBusMessageContext, bool> contextFilter)
        {
            ArgumentNullException.ThrowIfNull(contextFilter);
            _messageContextFilters.Add(contextFilter);

            return this;
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serializer"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v4.0, please use " + nameof(UseMessageBodySerializer) + " which provides the exact same functionality")]
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(IMessageBodySerializer serializer)
        {
            return UseMessageBodySerializer(serializer);
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v4.0, please use " + nameof(UseMessageBodySerializer) + " which provides the exact same functionality")]
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(Func<IServiceProvider, IMessageBodySerializer> implementationFactory)
        {
            return UseMessageBodySerializer(implementationFactory);
        }
    }
}