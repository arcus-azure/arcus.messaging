using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    ///     Foundation for building message pumps
    /// </summary>
    public abstract class MessagePump : BackgroundService
    {
        /// <summary>
        ///     Default encoding used
        /// </summary>
        protected Encoding DefaultEncoding { get; } = Encoding.UTF8;

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
        /// <typeparam name="TMessageContext">Type of message context for the provider</typeparam>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected async Task ProcessMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(ServiceProvider);
            foreach (MessageHandler handler in handlers)
            {
                if (handler.TryParseToMessageFormat<TMessageContext>(message, out var result))
                {
                    if (result is null)
                    {
                        throw new InvalidCastException(
                            "Successful parsing from abstracted message to concrete message handler type did unexpectedly result in a 'null' parsing result");
                    }
                    
                    await handler.ProcessMessageAsync(result, messageContext, correlationInfo, cancellationToken);
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Message pump cannot correctly process the message in the '{typeof(TMessageContext)}' "
                + "because no 'IMessageHandler<,>' was registered in the dependency injection container. "
                + $"Make sure you call the correct '.With...' extension on the {nameof(IServiceCollection)} during the registration of the message pump to register a message handler");
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