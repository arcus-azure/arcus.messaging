using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="IMessageRouter{TMessageContext}"/> that can route Azure Service Bus <see cref="Message"/>s.
    /// </summary>
    public class AzureServiceBusMessageRouter : MessageRouter<AzureServiceBusMessageContext>, IAzureServiceBusMessageRouter
    {
        private readonly IAzureServiceBusFallbackMessageHandler _fallbackMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureServiceBusMessageRouter(
            IServiceProvider serviceProvider, 
            ILogger<AzureServiceBusMessageRouter> logger) 
            : base(serviceProvider, logger ?? NullLogger<AzureServiceBusMessageRouter>.Instance)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an service provider to retrieve the registered message handlers");

            _fallbackMessageHandler = serviceProvider.GetService<IAzureServiceBusFallbackMessageHandler>();
        }

        /// <summary>
        ///     Handle a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <remarks>
        ///     Note that registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s with specific Azure Service Bus operations, will not be able to call those operations
        ///     without an <see cref="MessageReceiver"/>. Use the <see cref="ProcessMessageAsync(MessageReceiver,Message,AzureServiceBusMessageContext,MessageCorrelationInfo,CancellationToken)"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        public async Task ProcessMessageAsync(
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to be processed by the registered message handlers");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an Azure Service Bus message context in which the incoming message can be processed");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires an correlation information to correlate between incoming Azure Service Bus messages");

            await ProcessMessageWithPotentialFallbackAsync(
                messageReceiver: null,
                message: message,
                messageContext: messageContext,
                correlationInfo: correlationInfo,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        ///     Handle a new message that was received.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The instance that can receive Azure Service Bus <see cref="Message"/>; used within <see cref="IMessageHandler{TMessage,TMessageContext}"/>s with Azure Service Bus specific operations.
        /// </param>
        /// <param name="message">The message that was received by the <paramref name="messageReceiver"/>.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        public async Task ProcessMessageAsync(
            MessageReceiver messageReceiver,
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(messageReceiver, nameof(messageReceiver), "Requires an Azure Service Bus message receiver while processing the message, so message handlers can call Azure Service Bus specific operations");
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to be processed by the registered message handlers");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an Azure Service Bus message context in which the incoming message can be processed");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires an correlation information to correlate between incoming Azure Service Bus messages");

            await ProcessMessageWithPotentialFallbackAsync(messageReceiver, message, messageContext, correlationInfo, cancellationToken);
        }

        private async Task ProcessMessageWithPotentialFallbackAsync(
            MessageReceiver messageReceiver,
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IEnumerable<MessageHandler> messageHandlers = GetRegisteredMessageHandlers();
            if (!messageHandlers.Any() && _fallbackMessageHandler is null)
            {
                throw new InvalidOperationException(
                    "Azure Service Bus message pump cannot correctly process the message in the message context "
                    + "because no 'IMessageHandler<,>' was registered in the dependency injection container. "
                    + $"Make sure you call the correct '.With...' extension on the {nameof(IServiceCollection)} during the registration of the message pump to register a message handler");
            }

            Encoding encoding = messageContext.GetMessageEncodingProperty(Logger);
            string messageBody = encoding.GetString(message.Body);

            foreach (MessageHandler messageHandler in messageHandlers)
            {
                MessageResult result = await DeserializeMessageForHandlerAsync(messageBody, messageContext, messageHandler);
                if (result.IsSuccess)
                {
                    PreProcessMessage(messageReceiver, messageHandler, messageContext);
                    await messageHandler.ProcessMessageAsync(result.DeserializedMessage, messageContext, correlationInfo, cancellationToken);
                    return;
                }
            }

            Logger.LogInformation(
                "Fallback on registered {FallbackMessageHandlerType} because none of the message handlers were able to process the message",
                nameof(IAzureServiceBusFallbackMessageHandler));

            if (_fallbackMessageHandler is null)
            {
                throw new InvalidOperationException(
                    $"Message pump cannot correctly process the message in the '{nameof(AzureServiceBusMessageContext)}' "
                    + "because none of the registered 'IAzureServiceBusMessageHandler<,>' implementations in the dependency injection container matches the incoming message type and context. "
                    + $"Make sure you call the correct '.WithServiceBusMessageHandler' extension on the {nameof(IServiceCollection)} during the registration of the message pump to register a message handler");
            }

            await _fallbackMessageHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
            Logger.LogTrace("Fallback message handler has processed the message");
        }

        private void PreProcessMessage(
            MessageReceiver messageReceiver,
            MessageHandler messageHandler,
            AzureServiceBusMessageContext messageContext)
        {
            object messageHandlerInstance = messageHandler.GetMessageHandlerInstance();
            Type messageHandlerType = messageHandlerInstance.GetType();

            Logger.LogTrace("Start pre-processing message handler {MessageHandlerType}...", messageHandlerType.Name);

            if (messageReceiver != null && messageHandlerInstance is AzureServiceBusMessageHandlerTemplate template)
            {
                template.SetLockToken(messageContext.SystemProperties.LockToken);
                template.SetMessageReceiver(messageReceiver);
            }
            else
            {
                Logger.LogTrace("Nothing to pre-process for message handler type '{MessageHandlerType}'", messageHandlerType.Name);
            }
        }
    }
}
