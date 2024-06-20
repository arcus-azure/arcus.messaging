using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Observability.Telemetry.Core;
using Arcus.Messaging.Abstractions.Telemetry;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Context;
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
            : this(serviceProvider, options, (ILogger) logger)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an service provider to retrieve the registered message handlers");
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureServiceBusMessageRouter(IServiceProvider serviceProvider, AzureServiceBusMessageRouterOptions options)
            : this(serviceProvider, options, NullLogger.Instance)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an service provider to retrieve the registered message handlers");
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureServiceBusMessageRouter(IServiceProvider serviceProvider, ILogger<AzureServiceBusMessageRouter> logger)
            : this(serviceProvider, new AzureServiceBusMessageRouterOptions(), (ILogger) logger)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an service provider to retrieve the registered message handlers");
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureServiceBusMessageRouter(IServiceProvider serviceProvider)
            : this(serviceProvider, new AzureServiceBusMessageRouterOptions(), NullLogger.Instance)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an service provider to retrieve the registered message handlers");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected AzureServiceBusMessageRouter(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, new AzureServiceBusMessageRouterOptions(), logger)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an service provider to retrieve the registered message handlers");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected AzureServiceBusMessageRouter(IServiceProvider serviceProvider, AzureServiceBusMessageRouterOptions options, ILogger logger)
            : base(serviceProvider, options, logger ?? NullLogger<AzureServiceBusMessageRouter>.Instance)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires an service provider to retrieve the registered message handlers");

            ServiceBusOptions = options;
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the Azure Service Bus router.
        /// </summary>
        protected AzureServiceBusMessageRouterOptions ServiceBusOptions { get; }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        public override async Task RouteMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var isSuccessful = false;
            using (DurationMeasurement measurement = DurationMeasurement.Start())
            {
                try
                {
                    await base.RouteMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                    isSuccessful = true;
                }
                finally
                {
                    Logger.LogServiceBusRequest(
                        serviceBusNamespace: "<not-available>", 
                        entityName: "<not-available>", 
                        Options.Telemetry.OperationName, 
                        isSuccessful, 
                        measurement, 
                        ServiceBusEntityType.Unknown);
                }
            }
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> or <see cref="IAzureServiceBusFallbackMessageHandler"/>
        /// if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <remarks>
        ///     Note that registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s with specific Azure Service Bus operations (dead-letter, complete...),
        ///     will not be able to call those operations without an <see cref="ServiceBusReceiver"/>.
        ///     Use the <see cref="RouteMessageAsync(ServiceBusReceiver,ServiceBusReceivedMessage,AzureServiceBusMessageContext,MessageCorrelationInfo,CancellationToken)"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        public async Task RouteMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to be processed by the registered message handlers");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an Azure Service Bus message context in which the incoming message can be processed");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires an correlation information to correlate between incoming Azure Service Bus messages");

            await RouteMessageWithPotentialFallbackAsync(
                messageReceiver: null,
                message: message,
                messageContext: messageContext,
                correlationInfo: correlationInfo,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> or <see cref="IAzureServiceBusFallbackMessageHandler"/>
        /// if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The receiver that can call operations (dead letter, complete...) on an Azure Service Bus <see cref="ServiceBusReceivedMessage"/>;
        ///     used within <see cref="AzureServiceBusMessageHandler{TMessage}"/>s or <see cref="AzureServiceBusFallbackMessageHandler"/>s.
        /// </param>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        public async Task RouteMessageAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(messageReceiver, nameof(messageReceiver), "Requires an Azure Service Bus message receiver while processing the message, so message handlers can call Azure Service Bus specific operations");
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to be processed by the registered message handlers");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an Azure Service Bus message context in which the incoming message can be processed");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires an correlation information to correlate between incoming Azure Service Bus messages");

            await RouteMessageWithPotentialFallbackAsync(messageReceiver, message, messageContext, correlationInfo, cancellationToken);
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> or <see cref="IAzureServiceBusFallbackMessageHandler"/>
        /// if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The receiver that can call operations (dead letter, complete...) on an Azure Service Bus <see cref="ServiceBusReceivedMessage"/>;
        ///     used within <see cref="AzureServiceBusMessageHandler{TMessage}"/>s or <see cref="AzureServiceBusFallbackMessageHandler"/>s.
        /// </param>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        protected async Task RouteMessageWithPotentialFallbackAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to be processed by the registered message handlers");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an Azure Service Bus message context in which the incoming message can be processed");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires an correlation information to correlate between incoming Azure Service Bus messages");

            var isSuccessful = false;
            using (DurationMeasurement measurement = DurationMeasurement.Start())
            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
            using (LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfo, Options.CorrelationEnricher)))
            {
                try
                {
                    var accessor = serviceScope.ServiceProvider.GetService<IMessageCorrelationInfoAccessor>();
                    accessor?.SetCorrelationInfo(correlationInfo);

                    await TryRouteMessageWithPotentialFallbackAsync(serviceScope.ServiceProvider, messageReceiver, message, messageContext, correlationInfo, cancellationToken);
                    isSuccessful = true;
                }
                finally
                {
                    string entityName = messageReceiver?.EntityPath ?? "<not-available>";
                    string serviceBusNamespace = messageReceiver?.FullyQualifiedNamespace ?? "<not-available>";
                    Logger.LogServiceBusRequest(serviceBusNamespace, entityName, Options.Telemetry.OperationName, isSuccessful, measurement, messageContext.EntityType);
                }
            }
        }

        private async Task TryRouteMessageWithPotentialFallbackAsync(
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
                EnsureAnyMessageHandlerAvailable(messageHandlers, messageContext);

                Encoding encoding = messageContext.GetMessageEncodingProperty(Logger);
                string messageBody = encoding.GetString(message.Body.ToArray());
                bool hasGoneThroughMessageHandler = false;

                foreach (MessageHandler messageHandler in messageHandlers)
                {
                    MessageResult deserializeResult = await DeserializeMessageForHandlerAsync(messageBody, messageContext, messageHandler);
                    if (deserializeResult.IsSuccess)
                    {
                        var args = new ProcessMessageEventArgs(message, messageReceiver, cancellationToken);
                        SetServiceBusPropertiesForSpecificOperations(messageHandler, args, messageContext);

                        bool isProcessed = 
                            await messageHandler.ProcessMessageAsync(deserializeResult.DeserializedMessage, messageContext, correlationInfo, cancellationToken);

                        hasGoneThroughMessageHandler = true;
                        if (isProcessed)
                        {
                            return;
                        }
                    }
                }

                if (!hasGoneThroughMessageHandler)
                {
                    EnsureFallbackMessageHandlerAvailable(messageContext); 
                }

                await TryFallbackProcessMessageAsync(messageBody, messageContext, correlationInfo, cancellationToken);
                await TryServiceBusFallbackMessageAsync(messageReceiver, message, messageContext, correlationInfo, cancellationToken);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unable to process message with ID '{MessageId}'", message.MessageId);
                throw;
            }
        }

        private void EnsureAnyMessageHandlerAvailable(MessageHandler[] messageHandlers, AzureServiceBusMessageContext messageContext)
        {
            ServiceBusFallbackMessageHandler[] serviceBusFallbackHandlers = 
                GetAvailableFallbackMessageHandlersByContext<ServiceBusReceivedMessage, AzureServiceBusMessageContext>(messageContext);

            FallbackMessageHandler<string, MessageContext>[] generalFallbackHandlers = 
                GetAvailableFallbackMessageHandlersByContext<string, MessageContext>(messageContext);

            if (messageHandlers.Length <= 0 && serviceBusFallbackHandlers.Length <= 0 && generalFallbackHandlers.Length <= 0)
            {
                throw new InvalidOperationException(
                    $"Azure Service Bus message router cannot correctly process the message in the '{nameof(AzureServiceBusMessageContext)}' "
                    + "because no 'IAzureServiceBusMessageHandler<>' was registered in the dependency injection container. "
                    + $"Make sure you call the correct 'WithServiceBusMessageHandler' extension on the {nameof(IServiceCollection)} "
                    + "during the registration of the Azure Service Bus message pump or message router to register a message handler");
            }
        }

        private void EnsureFallbackMessageHandlerAvailable(AzureServiceBusMessageContext messageContext)
        {
            ServiceBusFallbackMessageHandler[] serviceBusFallbackHandlers = 
                GetAvailableFallbackMessageHandlersByContext<ServiceBusReceivedMessage, AzureServiceBusMessageContext>(messageContext);

            FallbackMessageHandler<string, MessageContext>[] generalFallbackHandlers = 
                GetAvailableFallbackMessageHandlersByContext<string, MessageContext>(messageContext);

            if (serviceBusFallbackHandlers.Length <= 0 && generalFallbackHandlers.Length <= 0)
            {
                throw new InvalidOperationException(
                    $"Azure Service Bus message router cannot correctly process the message in the '{nameof(AzureServiceBusMessageContext)}' "
                    + "because none of the registered 'IAzureServiceBusMessageHandler<,>' implementations in the dependency injection container matches the incoming message type and context; "
                    + $"and no '{nameof(IFallbackMessageHandler)}' or '{nameof(IAzureServiceBusFallbackMessageHandler)}' was registered to fall back to."
                    + $"Make sure you call the correct '.WithServiceBusMessageHandler' extension on the {nameof(IServiceCollection)} during the registration of the message pump or message router to register a message handler");
            }
        }

        /// <summary>
        /// Sets the Azure Service Bus properties on registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// that implements the <see cref="AzureServiceBusMessageHandler{TMessage}"/> for calling specific Service Bus operations during the message processing.
        /// </summary>
        /// <param name="messageHandler">The message handler on which the Service Bus properties should be set.</param>
        /// <param name="eventArgs">The event args of the incoming Service Bus message.</param>
        /// <param name="messageContext">The context in which the received Service Bus message is processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageHandler"/> or <paramref name="messageContext"/> is <c>null</c>.</exception>
        protected void SetServiceBusPropertiesForSpecificOperations(
            MessageHandler messageHandler,
            ProcessMessageEventArgs eventArgs,
            AzureServiceBusMessageContext messageContext)
        {
            Guard.NotNull(messageHandler, nameof(messageHandler), "Requires an Azure Service Bus message handler to set the specific Service Bus properties");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an Azure Service Bus message context in which the incoming message can be processed");
            
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
        protected async Task TryServiceBusFallbackMessageAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to be processed by the registered fallback message handler");
            Guard.NotNull(messageContext, nameof(messageContext), "Requires an Azure Service Bus message context in which the incoming message can be processed");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires an correlation information to correlate between incoming Azure Service Bus messages");

            ServiceBusFallbackMessageHandler[] fallbackHandlers = 
                GetAvailableFallbackMessageHandlersByContext<ServiceBusReceivedMessage, AzureServiceBusMessageContext>(messageContext);
            
            if (fallbackHandlers.Length <= 0)
            {
                Logger.LogTrace("No Azure Service Bus message handlers found within message context (JobId: {JobId})", messageContext.JobId);
                return;
            }

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
                    break;
                }

                Logger.LogTrace("Fallback message handler '{FallbackMessageHandlerType}' was not able to process the message", fallbackMessageHandlerTypeName);
            }
        }
    }
}
