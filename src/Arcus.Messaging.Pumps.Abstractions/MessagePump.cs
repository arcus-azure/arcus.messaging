using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    ///     Foundation for building message pumps
    /// </summary>
    /// <typeparam name="TMessage">Type of message we are interested in</typeparam>
    /// <typeparam name="TMessageContext">Type of message context for the provider</typeparam>
    public abstract class MessagePump<TMessage, TMessageContext> : BackgroundService
        where TMessageContext : MessageContext
    {
        /// <summary>
        ///     Unique id of this message pump instance
        /// </summary>
        protected string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     Id of the client being used to connect to the messaging service
        /// </summary>
        protected string ClientId { get; private set; }

        /// <summary>
        ///     Entity path that is being processed
        /// </summary>
        protected string EntityPath { get; private set; }

        /// <summary>
        ///     Logger to write telemetry to
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        ///     Configuration of the application
        /// </summary>
        protected IConfiguration Configuration { get; }


        /// <summary>
        ///     Collection of services that are configured
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        protected MessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            Logger = logger;
            Configuration = configuration;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        ///     Handles an exception that occured during the receiving of a message
        /// </summary>
        /// <param name="receiveException">Exception that occured</param>
        protected virtual Task HandleReceiveExceptionAsync(Exception receiveException)
        {
            Logger.LogCritical(receiveException, "Unable to process message from {EntityPath} with client {ClientId}", EntityPath, ClientId);
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Handle a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected async Task ProcessMessageAsync<TMessageHandler>(
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageHandler : IMessageHandler<TMessage, TMessageContext>
        {
            IEnumerable<TMessageHandler> messageHandlers = ServiceProvider.GetServices<TMessageHandler>();
            if (messageHandlers is null || !messageHandlers.Any())
            {
                throw new InvalidOperationException(
                    $"Message pump cannot correctly process the '{typeof(TMessage).Name}' in the '{typeof(TMessageContext)}' "
                    + $"because no '{nameof(IMessageHandler<TMessage, TMessageContext>)}' was registered in the dependency injection container. "
                    + $"Make sure you call the correct '.With...' extension on the {nameof(IServiceCollection)} during the registration of the message pump to register a message handler");
            }

            foreach (TMessageHandler messageHandler in messageHandlers)
            {
                if (messageHandler is null)
                {
                    continue;
                }

                bool isProcessed = await TryProcessMessageAsync(messageHandler, message, messageContext, correlationInfo, cancellationToken);
                if (isProcessed)
                {
                    break;
                }
            }
        }

        private async Task<bool> TryProcessMessageAsync(
            IMessageHandler<TMessage, TMessageContext> messageHandler,
            TMessage message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Start try processing message:{messageType} with message handler:{messageHandler}", typeof(TMessage).Name, messageHandler.GetType().Name);
            MessageProcessResult processResult = 
                await messageHandler.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);

            switch (processResult)
            {
                case MessageProcessResult.Processed: 
                    Logger.LogInformation(
                        "Message:{messageType} was processed correctly by message handler:{messageHandler}", 
                        typeof(TMessage).Name, messageHandler.GetType().Name);
                    return true;
                case MessageProcessResult.NotSupported:
                    Logger.LogWarning(
                        "Message:{messageType} was not correctly processed by message handler:{messageHandler}, because the handler don't support it", 
                        typeof(TMessage).Name, messageHandler.GetType().Name);
                    return false;
                case MessageProcessResult.Failure:
                    Logger.LogError(
                        "Message:{messageType} failed to be processed by message handler:{messageHandler}, because of a failure in the handler", 
                        typeof(TMessage).Name, messageHandler.GetType().Name);
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(processResult), 
                        processResult, 
                        $"Message handler '{messageHandler.GetType().Name}' returned unknown message process result");
            }
        }

        /// <summary>
        ///     Triggered when the message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogWarning("Host is shutting down");

            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        ///     Register information about the client connected to the messaging service
        /// </summary>
        /// <param name="clientId">Id of the client being used to connect to the messaging service</param>
        /// <param name="entityPath">Entity path that is being processed</param>
        protected void RegisterClientInformation(string clientId, string entityPath)
        {
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));

            ClientId = clientId;
            EntityPath = entityPath;
        }
    }
}