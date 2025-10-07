using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Arcus.Messaging.Abstractions.MessageHandling.MessageProcessingError;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents an <see cref="IAzureServiceBusMessageRouter"/> that can route Azure Service Bus <see cref="ServiceBusReceivedMessage"/>s.
    /// </summary>
    internal class ServiceBusMessageRouter : MessageRouter
    {
        private readonly AzureServiceBusMessageRouterOptions _serviceBusOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        internal ServiceBusMessageRouter(IServiceProvider serviceProvider, AzureServiceBusMessageRouterOptions options, ILogger<ServiceBusMessageRouter> logger)
            : base(serviceProvider, options, logger)
        {
            _serviceBusOptions = options;
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IServiceBusMessageHandler{TMessage}"/>s.
        /// </summary>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        internal async Task<MessageProcessingResult> RouteMessageAsync(
            ServiceBusReceivedMessage message,
            ServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            using IServiceScope serviceScope = ServiceProvider.CreateScope();
            using var _ = Logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = messageContext.JobId,
                ["Service Bus namespace"] = messageContext.FullyQualifiedNamespace,
                ["Service Bus entity name"] = messageContext.EntityPath
            });

            Logger.LogDebug("[Received] message (message ID={MessageId}) on Azure Service Bus {EntityType} message pump", messageContext.MessageId, messageContext.EntityType);

            string messageBody = LoadMessageBody(message, messageContext);
            MessageProcessingResult result =
                await RouteMessageThroughRegisteredHandlersAsync(serviceScope.ServiceProvider, messageBody, messageContext, correlationInfo, cancellationToken);

            if (result.IsSuccessful)
            {
                await PotentiallyAutoCompleteMessageAsync(messageContext);
            }
            else
            {
                switch (result.Error)
                {
                    case ProcessingInterrupted:
                    case MatchedHandlerFailed:
                        Logger.LogDebug("[Settle:Abandon] message (message ID={MessageId}) on Azure Service Bus {EntityType} message pump => {ErrorMessage}", messageContext.MessageId, messageContext.EntityType, result.ErrorMessage);
                        await messageContext.AbandonMessageAsync(new Dictionary<string, object>(), CancellationToken.None);
                        break;

                    case CannotFindMatchedHandler:
                        Logger.LogDebug("[Settle:DeadLetter] message (message ID={MessageId}) on Azure Service Bus {EntityType} message pump => {ErrorMessage}", messageContext.MessageId, messageContext.EntityType, result.ErrorMessage);
                        await messageContext.DeadLetterMessageAsync(CannotFindMatchedHandler.ToString(), result.ErrorMessage, CancellationToken.None);
                        break;
                }
            }

            return result;
        }

        private static string LoadMessageBody(ServiceBusReceivedMessage message, ServiceBusMessageContext context)
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

        private async Task PotentiallyAutoCompleteMessageAsync(ServiceBusMessageContext messageContext)
        {
            if (_serviceBusOptions.AutoComplete)
            {
                try
                {
                    await messageContext.CompleteMessageAsync(CancellationToken.None);
                }
                catch (ServiceBusException exception) when (exception.Reason is ServiceBusFailureReason.MessageLockLost)
                {
                    Logger.LogTrace(exception, "[Skipped] auto-completion of message '{MessageId}' in Azure Service Bus message pump (already settled)", messageContext.MessageId);
                }
            }
        }
    }
}
