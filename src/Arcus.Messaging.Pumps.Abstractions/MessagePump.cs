using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    /// Represents the foundation for building message pumps.
    /// </summary>
    public abstract class MessagePump : BackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePump"/> class.
        /// </summary>
        /// <param name="configuration">The configuration of the application.</param>
        /// <param name="serviceProvider">The collection of services that are configured.</param>
        /// <param name="logger">The logger to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, the <paramref name="serviceProvider"/>, or <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        protected MessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(serviceProvider, nameof(serviceProvider));
            Guard.NotNull(logger, nameof(logger));

            Logger = logger;
            Configuration = configuration;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; protected set; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     Unique id of this message pump instance
        /// </summary>
        [Obsolete("Use the " + nameof(JobId) + " instead to identify the message pump")]
        protected string Id => JobId;

        /// <summary>
        /// Gets hte ID of the client being used to connect to the messaging service.
        /// </summary>
        protected string ClientId { get; private set; }

        /// <summary>
        /// Gets entity path that is being processed.
        /// </summary>
        public string EntityPath { get; private set; }

        /// <summary>
        /// Gets the configuration of the application.
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the collection of application services that are configured.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the default encoding used during the message processing through the message pump.
        /// </summary>
        protected Encoding DefaultEncoding { get; } = Encoding.UTF8;

        /// <summary>
        /// Gets the logger to write telemetry to.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Handles an exception that occurred during the receiving of a message
        /// </summary>
        /// <param name="receiveException">Exception that occurred</param>
        protected virtual Task HandleReceiveExceptionAsync(Exception receiveException)
        {
            Logger.LogCritical(receiveException, "Unable to process message from {EntityPath} with client {ClientId}", EntityPath, ClientId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogWarning("Host is shutting down");

            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Start with receiving messages on this message pump.
        /// </summary>
        /// <param name="cancellationToken">The token to indicate the start process should no longer be graceful.</param>
        public virtual Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop with receiving messages on this message pump.
        /// </summary>
        /// <param name="cancellationToken">The token to indicate the stop process should no longer be graceful.</param>
        public virtual Task StopProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Register information about the client connected to the messaging service
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