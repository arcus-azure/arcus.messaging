using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.Telemetry;
using Arcus.Observability.Telemetry.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Context;
using static Arcus.Messaging.Abstractions.MessageHandling.MessageProcessingError;
using ServiceBusFallbackMessageHandler = Arcus.Messaging.Abstractions.MessageHandling.FallbackMessageHandler<Azure.Messaging.ServiceBus.ServiceBusReceivedMessage, Arcus.Messaging.Abstractions.ServiceBus.AzureServiceBusMessageContext>;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="IMessageRouter"/> that can route Azure Service Bus <see cref="ServiceBusReceivedMessage"/>s.
    /// </summary>
    public class AzureServiceBusMessageRouter : MessageRouter, IAzureServiceBusMessageRouter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureServiceBusMessageRouter(IServiceProvider serviceProvider, AzureServiceBusMessageRouterOptions options, ILogger<AzureServiceBusMessageRouter> logger)
            : base(serviceProvider, options, (ILogger) logger)
        {
            ServiceBusOptions = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        public AzureServiceBusMessageRouter(IServiceProvider serviceProvider, AzureServiceBusMessageRouterOptions options)
            : this(serviceProvider, options, NullLogger.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        public AzureServiceBusMessageRouter(IServiceProvider serviceProvider, ILogger<AzureServiceBusMessageRouter> logger)
            : this(serviceProvider, new AzureServiceBusMessageRouterOptions(), (ILogger) logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        public AzureServiceBusMessageRouter(IServiceProvider serviceProvider)
            : this(serviceProvider, new AzureServiceBusMessageRouterOptions(), NullLogger.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        protected AzureServiceBusMessageRouter(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, new AzureServiceBusMessageRouterOptions(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0 for simplified message router initialization")]
        protected AzureServiceBusMessageRouter(IServiceProvider serviceProvider, AzureServiceBusMessageRouterOptions options, ILogger logger)
            : base(serviceProvider, options, logger ?? NullLogger<AzureServiceBusMessageRouter>.Instance)
        {
            ServiceBusOptions = options;
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the Azure Service Bus router.
        /// </summary>
        protected AzureServiceBusMessageRouterOptions ServiceBusOptions { get; }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// and optionally through a <see cref="IAzureServiceBusFallbackMessageHandler"/>
        /// if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The receiver that can call operations (dead letter, complete...) on an Azure Service Bus <see cref="ServiceBusReceivedMessage"/>.
        /// </param>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        public async Task<MessageProcessingResult> RouteMessageAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
#pragma warning disable CS0618 // Type or member is obsolete: fallback handlers will be removed in v3.0.
            return await RouteMessageWithPotentialFallbackAsync(messageReceiver, message, messageContext, correlationInfo, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// and optionally through a registered <see cref="IAzureServiceBusFallbackMessageHandler"/>
        /// if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The receiver that can call operations (dead letter, complete...) on an Azure Service Bus <see cref="ServiceBusReceivedMessage"/>.
        /// </param>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        [Obsolete("Will be removed in v3.0, please use the Azure service bus operations on the " + nameof(AzureServiceBusMessageContext) + " instead of defining fallback message handlers")]
        protected async Task<MessageProcessingResult> RouteMessageWithPotentialFallbackAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            if (correlationInfo is null)
            {
                throw new ArgumentNullException(nameof(correlationInfo));
            }

            string entityName = messageReceiver?.EntityPath ?? "<not-available>";
            string serviceBusNamespace = messageReceiver?.FullyQualifiedNamespace ?? "<not-available>";

            using DurationMeasurement measurement = DurationMeasurement.Start();
            using IServiceScope serviceScope = ServiceProvider.CreateScope();
#pragma warning disable CS0618 // Type or member is obsolete: will be refactored when moving towards v3.0.
            using IDisposable _ = LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfo, Options.CorrelationEnricher));
#pragma warning restore CS0618 // Type or member is obsolete

            try
            {
#pragma warning disable CS0618 // Type or member is obsolete: will be refactored when moving towards v3.0.
                var accessor = serviceScope.ServiceProvider.GetService<IMessageCorrelationInfoAccessor>();
#pragma warning restore CS0618 // Type or member is obsolete
                accessor?.SetCorrelationInfo(correlationInfo);

                MessageProcessingResult routingResult = await TryRouteMessageWithPotentialFallbackAsync(serviceScope.ServiceProvider, messageReceiver, message, messageContext, correlationInfo, cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete: specific telemetry calls will be removed in v3.0.
                Logger.LogServiceBusRequest(serviceBusNamespace, entityName, Options.Telemetry.OperationName, routingResult.IsSuccessful, measurement, messageContext.EntityType);
#pragma warning restore CS0618 // Type or member is obsolete
                return routingResult;
            }
            catch
            {
#pragma warning disable CS0618 // Type or member is obsolete: specific telemery calls will be removed in v3.0.
                Logger.LogServiceBusRequest(serviceBusNamespace, entityName, Options.Telemetry.OperationName, false, measurement, messageContext.EntityType);
#pragma warning restore CS0618 // Type or member is obsolete
                throw;
            }
        }

        private async Task<MessageProcessingResult> TryRouteMessageWithPotentialFallbackAsync(
            IServiceProvider serviceProvider,
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                MessageHandler[] messageHandlers = GetRegisteredMessageHandlers(serviceProvider).ToArray();
                if (messageHandlers.Length <= 0)
                {
                    await DeadLetterMessageNoHandlerRegisteredAsync(messageReceiver, message, messageContext);
                    return MessageProcessingResult.Failure(message.MessageId, CannotFindMatchedHandler, "Failed to process message in the message pump as no message handler is registered in the dependency container");
                }

                string messageBody = LoadMessageBody(message, messageContext);
                bool hasGoneThroughMessageHandler = false;

                foreach (MessageHandler messageHandler in messageHandlers)
                {
                    MessageResult result = await DeserializeMessageForHandlerAsync(messageBody, messageContext, messageHandler);
                    if (result.IsSuccess)
                    {
                        var args = new ProcessMessageEventArgs(message, messageReceiver, cancellationToken);
#pragma warning disable CS0618 // Type or member is obsolete: Azure Service bus-specific message handler templates will be removed in v3.0.
                        SetServiceBusPropertiesForSpecificOperations(messageHandler, args, messageContext);
#pragma warning restore CS0618 // Type or member is obsolete

                        bool isProcessed = await messageHandler.ProcessMessageAsync(result.DeserializedMessage, messageContext, correlationInfo, cancellationToken);

                        hasGoneThroughMessageHandler = true;
                        if (isProcessed)
                        {
                            return MessageProcessingResult.Success(message.MessageId);
                        }
                    }
                }

#pragma warning disable CS0618 // Type or member is obsolete: fallback message handlers will be removed in v3.0.
                ServiceBusFallbackMessageHandler[] serviceBusFallbackHandlers =
                    GetAvailableFallbackMessageHandlersByContext<ServiceBusReceivedMessage, AzureServiceBusMessageContext>(messageContext);

                FallbackMessageHandler<string, MessageContext>[] generalFallbackHandlers =
                    GetAvailableFallbackMessageHandlersByContext<string, MessageContext>(messageContext);
#pragma warning restore CS0618 // Type or member is obsolete

                bool fallbackAvailable = serviceBusFallbackHandlers.Length > 0 || generalFallbackHandlers.Length > 0;

                if (hasGoneThroughMessageHandler && !fallbackAvailable)
                {
                    await AbandonMessageMatchedHandlerFailedAsync(messageReceiver, message, messageContext);
                    return MessageProcessingResult.Failure(message.MessageId, MatchedHandlerFailed, "Failed to process Azure Service Bus message in pump as the matched handler did not successfully processed the message and no fallback message handlers were configured");
                }

                if (!hasGoneThroughMessageHandler && !fallbackAvailable)
                {
                    await DeadLetterMessageNoHandlerMatchedAsync(messageReceiver, message, messageContext);
                    return MessageProcessingResult.Failure(message.MessageId, CannotFindMatchedHandler, "Failed to process message in pump as no message handler was matched against the message and no fallback message handlers were configured");
                }

#pragma warning disable CS0618 // Type or member is obsolete: general message routing will be removed in v3.0.
                bool isProcessedByGeneralFallback = await TryFallbackProcessMessageAsync(messageBody, messageContext, correlationInfo, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                if (isProcessedByGeneralFallback)
                {
                    return MessageProcessingResult.Success(message.MessageId);
                }

#pragma warning disable CS0618 // Type or member is obsolete: fallback message handling will be removed in v3.0.
                return await TryServiceBusFallbackMessageAsync(messageReceiver, message, messageContext, correlationInfo, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete

            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unable to process message with ID '{MessageId}'", message.MessageId);
                if (messageReceiver != null)
                {
                    await messageReceiver.AbandonMessageAsync(message);
                }

                return MessageProcessingResult.Failure(message.MessageId, ProcessingInterrupted, "Failed to process message in pump as there was an unexpected critical problem during processing, please see the logs for more information", exception);
            }
        }

        private static string LoadMessageBody(ServiceBusReceivedMessage message, AzureServiceBusMessageContext context)
        {
            Encoding encoding = DetermineEncoding();
            string messageBody = encoding.GetString(message.Body.ToArray());

            return messageBody;

            Encoding DetermineEncoding()
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
        }

        private async Task DeadLetterMessageNoHandlerRegisteredAsync(ServiceBusReceiver messageReceiver, ServiceBusReceivedMessage message, AzureServiceBusMessageContext messageContext)
        {
            if (messageReceiver != null)
            {
                Logger.LogError("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as no message handlers were registered in the application services, dead-lettering message!", message.MessageId, messageContext.JobId);
                await messageReceiver.DeadLetterMessageAsync(message, "No message handlers were registered in the application services");
            }
        }

        private async Task DeadLetterMessageNoHandlerMatchedAsync(ServiceBusReceiver messageReceiver, ServiceBusReceivedMessage message, AzureServiceBusMessageContext messageContext)
        {
            if (messageReceiver != null)
            {
                Logger.LogError("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as no message handler was matched against the message and no fallback message handlers was configured, dead-lettering message!", message.MessageId, messageContext.JobId);
                await messageReceiver.DeadLetterMessageAsync(message, "No message handler was matched against the message and no fallback handlers were configured");
            }
        }

        private async Task AbandonMessageMatchedHandlerFailedAsync(ServiceBusReceiver messageReceiver, ServiceBusReceivedMessage message, AzureServiceBusMessageContext messageContext)
        {
            if (messageReceiver != null)
            {
                Logger.LogDebug("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as the matched message handler did not successfully process the message and no fallback message handlers configured, abandoning message!", message.MessageId, messageContext.JobId);
                await messageReceiver.AbandonMessageAsync(message);
            }
        }

        /// <summary>
        /// Sets the Azure Service Bus properties on registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.
        /// </summary>
        /// <param name="messageHandler">The message handler on which the Service Bus properties should be set.</param>
        /// <param name="eventArgs">The event args of the incoming Service Bus message.</param>
        /// <param name="messageContext">The context in which the received Service Bus message is processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandler"/> or <paramref name="messageContext"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0, please use the Azure service bus operations on the " + nameof(AzureServiceBusMessageContext) + " instead")]
        protected void SetServiceBusPropertiesForSpecificOperations(
            MessageHandler messageHandler,
            ProcessMessageEventArgs eventArgs,
            AzureServiceBusMessageContext messageContext)
        {
            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            object messageHandlerInstance = messageHandler.GetMessageHandlerInstance();
            Type messageHandlerType = messageHandlerInstance.GetType();

            if (messageHandlerInstance is AzureServiceBusMessageHandlerTemplate template)
            {
                if (eventArgs is null)
                {
                    Logger.LogWarning("Message handler '{MessageHandlerType}' uses specific Azure Service Bus operations, but is not able to be configured during message routing because the message router didn't receive a Azure Service Bus message receiver; use other '{RouteMessageOverload}' method overload", messageHandlerType.Name, nameof(RouteMessageAsync));
                }
                else
                {
                    template.SetProcessMessageEventArgs(eventArgs);
                }
            }
        }

        /// <summary>
        /// Tries to process the unhandled <paramref name="message"/> through an potential registered <see cref="IAzureServiceBusFallbackMessageHandler"/> instance.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The instance that can receive Azure Service Bus <see cref="ServiceBusReceivedMessage"/>; used within <see cref="IAzureServiceBusFallbackMessageHandler"/>s with Azure Service Bus specific operations.
        /// </param>
        /// <param name="message">The message that was received by the <paramref name="messageReceiver"/>.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        [Obsolete("Will be removed in v3.0, please use the Azure service bus operations on the " + nameof(AzureServiceBusMessageContext) + " instead of defining fallback message handlers")]
        protected async Task<MessageProcessingResult> TryServiceBusFallbackMessageAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            if (correlationInfo is null)
            {
                throw new ArgumentNullException(nameof(correlationInfo));
            }

            ServiceBusFallbackMessageHandler[] fallbackHandlers =
                GetAvailableFallbackMessageHandlersByContext<ServiceBusReceivedMessage, AzureServiceBusMessageContext>(messageContext);

            foreach (ServiceBusFallbackMessageHandler handler in fallbackHandlers)
            {
                if (handler.MessageHandlerInstance is AzureServiceBusMessageHandlerTemplate template)
                {
                    if (messageReceiver is null)
                    {
                        Logger.LogWarning("Fallback message handler '{MessageHandlerType}' uses specific Azure Service Bus operations, but is unable to be configured during message routing because the message router didn't receive a Azure Service Bus message receiver; use other '{MethodName}' method overload", handler.MessageHandlerType.Name, nameof(RouteMessageAsync));
                    }
                    else
                    {
                        var args = new ProcessMessageEventArgs(message, messageReceiver, cancellationToken);
                        template.SetProcessMessageEventArgs(args);
                    }
                }

                string fallbackMessageHandlerTypeName = handler.MessageHandlerType.Name;
                Logger.LogTrace("Fallback on registered '{FallbackMessageHandlerType}' because none of the message handlers were able to process the message", fallbackMessageHandlerTypeName);

                bool result = await handler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                if (result)
                {
                    Logger.LogTrace("Fallback message handler '{FallbackMessageHandlerType}' has processed the message", fallbackMessageHandlerTypeName);
                    return MessageProcessingResult.Success(message.MessageId);
                }

                Logger.LogTrace("Fallback message handler '{FallbackMessageHandlerType}' was not able to process the message", fallbackMessageHandlerTypeName);
            }

            if (messageReceiver != null)
            {
                Logger.LogWarning("No fallback message handler processed the Azure Service Bus message '{MessageId}' in pump '{JobId}', abandoning message!", message.MessageId, messageContext.JobId);
                await messageReceiver.AbandonMessageAsync(message);
            }

            return MessageProcessingResult.Failure(message.MessageId, CannotFindMatchedHandler, "No fallback message handler processed the message");
        }
    }
}
