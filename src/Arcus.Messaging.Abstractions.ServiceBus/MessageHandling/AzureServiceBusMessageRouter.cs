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
using Serilog.Context;
using static Arcus.Messaging.Abstractions.MessageHandling.MessageProcessingError;

#pragma warning disable S1133

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="IAzureServiceBusMessageRouter"/> that can route Azure Service Bus <see cref="ServiceBusReceivedMessage"/>s.
    /// </summary>
    [Obsolete("Will be removed in v4.0 as the message router is being made internal")]
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
            : base(serviceProvider, options, logger)
        {
            ServiceBusOptions = options;
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the Azure Service Bus router.
        /// </summary>
        protected AzureServiceBusMessageRouterOptions ServiceBusOptions { get; }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.
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
        [Obsolete("Will be removed in v3.0")]
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
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.
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
        [Obsolete("Will be removed in v3.0, please use the Azure service bus operations on the " + nameof(ServiceBusMessageContext) + " instead of defining fallback message handlers")]
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
                        bool isProcessed = await messageHandler.ProcessMessageAsync(result.DeserializedMessage, messageContext, correlationInfo, cancellationToken);

                        hasGoneThroughMessageHandler = true;
                        if (isProcessed)
                        {
                            await PotentiallyAutoCompleteMessageAsync(messageReceiver, message, messageContext);
                            return MessageProcessingResult.Success(message.MessageId);
                        }
                    }
                }

                if (hasGoneThroughMessageHandler)
                {
                    await AbandonMessageMatchedHandlerFailedAsync(messageReceiver, message, messageContext);
                    return MessageProcessingResult.Failure(message.MessageId, MatchedHandlerFailed, "Failed to process Azure Service Bus message in pump as the matched handler did not successfully processed the message");
                }

                await DeadLetterMessageNoHandlerMatchedAsync(messageReceiver, message, messageContext);
                return MessageProcessingResult.Failure(message.MessageId, CannotFindMatchedHandler, "Failed to process Azure Service Bus message in pump as no message handler was matched against the message");
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

        private async Task PotentiallyAutoCompleteMessageAsync(ServiceBusReceiver messageReceiver, ServiceBusReceivedMessage message, AzureServiceBusMessageContext messageContext)
        {
            if (ServiceBusOptions.AutoComplete)
            {
                try
                {
                    Logger.LogTrace("Auto-complete message '{MessageId}' (if needed) after processing in Azure Service Bus in message pump '{JobId}'", message.MessageId, messageContext.JobId);
                    await messageReceiver.CompleteMessageAsync(message);
                }
                catch (ServiceBusException exception) when (
                    exception.Message.Contains("lock")
                    && exception.Message.Contains("expired")
                    && exception.Message.Contains("already")
                    && exception.Message.Contains("removed"))
                {
                    Logger.LogTrace(exception, "Message '{MessageId}' on Azure Service Bus in message pump '{JobId}' does not need to be auto-completed, because it was already settled", message.MessageId, messageContext.JobId);
                }
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
                Logger.LogError("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as no registered message handler was matched against the message, dead-lettering message!", message.MessageId, messageContext.JobId);
                await messageReceiver.DeadLetterMessageAsync(message, "No registered message handler was matched against the message");
            }
        }

        private async Task AbandonMessageMatchedHandlerFailedAsync(ServiceBusReceiver messageReceiver, ServiceBusReceivedMessage message, AzureServiceBusMessageContext messageContext)
        {
            if (messageReceiver != null)
            {
                Logger.LogDebug("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as the matched message handler did not successfully process the message, abandoning message!", message.MessageId, messageContext.JobId);
                await messageReceiver.AbandonMessageAsync(message);
            }
        }
    }
}
