using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Observability.Telemetry.Core;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.Messaging.Abstractions.EventHubs.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="IMessageRouter"/> that can route Azure EventHubs <see cref="EventData"/>s.
    /// </summary>
    public class AzureEventHubsMessageRouter : MessageRouter, IAzureEventHubsMessageRouter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider)
            : this(serviceProvider, NullLogger<AzureEventHubsMessageRouter>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider, ILogger<AzureEventHubsMessageRouter> logger) 
            : this(serviceProvider, new AzureEventHubsMessageRouterOptions(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider, AzureEventHubsMessageRouterOptions options)
            : this(serviceProvider, options, NullLogger<AzureEventHubsMessageRouter>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider, AzureEventHubsMessageRouterOptions options, ILogger<AzureEventHubsMessageRouter> logger) 
            : this(serviceProvider, options, (ILogger) logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected AzureEventHubsMessageRouter(IServiceProvider serviceProvider, ILogger logger) 
            : this(serviceProvider, new AzureEventHubsMessageRouterOptions(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected AzureEventHubsMessageRouter(IServiceProvider serviceProvider, AzureEventHubsMessageRouterOptions  options, ILogger logger) 
            : base(serviceProvider, options, logger)
        {
            EventHubsOptions = options ?? new AzureEventHubsMessageRouterOptions();
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the Azure Service Bus router.
        /// </summary>
        protected AzureEventHubsMessageRouterOptions EventHubsOptions { get; }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s
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
        public async Task RouteMessageAsync(
            EventData message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            using (var measurement = DurationMeasurement.Start())
            {
                string messageBody = message.Data.ToString();
                await base.RouteMessageAsync(messageBody, messageContext, correlationInfo, cancellationToken);
                
                // TODO: Log EventHubs request.
            }
        }

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
        public override Task RouteMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            using (var measurement = DurationMeasurement.Start())
            {
                return base.RouteMessageAsync(message, messageContext, correlationInfo, cancellationToken);
                
                // TODO: Log EventHubs request.
            }
        }
    }
}
