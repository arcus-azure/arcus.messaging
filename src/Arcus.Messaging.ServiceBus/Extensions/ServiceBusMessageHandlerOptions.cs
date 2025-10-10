using System;
using System.Threading.Tasks;
using Arcus.Messaging;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents the available options when registering an <see cref="IServiceBusMessageHandler{TMessage}"/>.
    /// </summary>
    /// <typeparam name="TMessage">The custom message type of the message handler.</typeparam>
    public class ServiceBusMessageHandlerOptions<TMessage> : MessageHandlerOptions<TMessage, ServiceBusMessageContext>
    {
        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <typeparam name="TDeserializer">The custom <see cref="IMessageBodyDeserializer"/> type load from the application services.</typeparam>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodyDeserializer<TDeserializer>()
            where TDeserializer : IMessageBodyDeserializer
        {
            return UseMessageBodyDeserializer(serviceProvider => serviceProvider.GetRequiredService<TDeserializer>());
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="deserializer"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodyDeserializer(IMessageBodyDeserializer deserializer)
        {
            ArgumentNullException.ThrowIfNull(deserializer);
            return UseMessageBodyDeserializer(_ => deserializer);
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodyDeserializer(Func<IServiceProvider, IMessageBodyDeserializer> implementationFactory)
        {
            UseBodyDeserializer(implementationFactory);
            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="bodyFilter"/> to only select a subset of messages, based on its body, that the registered message handler can handle.
        /// </summary>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodyFilter(Func<TMessage, bool> bodyFilter)
        {
            AddBodyFilter(bodyFilter);
            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="contextFilter"/> to only select a subset of messages, based on its context, that the registered message handler can handle.
        /// </summary>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageContextFilter(Func<ServiceBusMessageContext, bool> contextFilter)
        {
            AddContextFilter(contextFilter);
            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="contextFilter"/> to only select a subset of messages, based on its context, that the registered message handler can handle.
        /// </summary>
        [Obsolete("Will be removed in v4.0, please use " + nameof(ServiceBusMessageContext) + " instead")]
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageContextFilter(Func<AzureServiceBusMessageContext, bool> contextFilter)
        {
            ArgumentNullException.ThrowIfNull(contextFilter);
            return AddMessageContextFilter(context =>
            {
                var deprecated = new AzureServiceBusMessageContext(context);
                return contextFilter(deprecated);
            });
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serializer"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v4.0, please use " + nameof(UseMessageBodyDeserializer) + " which provides the exact same functionality")]
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(IMessageBodySerializer serializer)
        {
            return AddMessageBodySerializer(_ => serializer);
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v4.0, please use " + nameof(UseMessageBodyDeserializer) + " which provides the exact same functionality")]
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(Func<IServiceProvider, IMessageBodySerializer> implementationFactory)
        {
            return UseMessageBodyDeserializer(serviceProvider =>
            {
                var deprecated = implementationFactory(serviceProvider);
                return new DeprecatedMessageBodyDeserializerAdapter(deprecated);
            });
        }

        [Obsolete("Will be removed in v3.0")]
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
    }
}