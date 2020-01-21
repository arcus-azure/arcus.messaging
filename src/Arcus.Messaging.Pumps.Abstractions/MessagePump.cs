using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        ///     Deserializes a raw JSON message body
        /// </summary>
        /// <param name="rawMessageBody">Raw message body to deserialize</param>
        /// <param name="messageContext">Context concerning the message</param>
        /// <returns>Deserialized message</returns>
        protected virtual TMessage DeserializeJsonMessageBody(byte[] rawMessageBody, MessageContext messageContext)
        {
            Encoding encoding = DetermineMessageEncoding(messageContext);
            string serializedMessageBody = encoding.GetString(rawMessageBody);

            TMessage messageBody = JsonConvert.DeserializeObject<TMessage>(serializedMessageBody);
            if (messageBody == null)
            {
                Logger.LogError("Unable to deserialize to message contract {ContractName} for message {MessageBody}",
                    typeof(TMessage), rawMessageBody);
            }

            return messageBody;
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry & processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected abstract Task ProcessMessageAsync(TMessage message, TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken);

        /// <summary>
        ///     Determines the encoding used for a given message
        /// </summary>
        /// <remarks>If no encoding was specified, UTF-8 will be used by default</remarks>
        /// <param name="messageContext">Context concerning the message</param>
        /// <returns>Encoding that was used for the message body</returns>
        protected virtual Encoding DetermineMessageEncoding(MessageContext messageContext)
        {
            if (messageContext.Properties.TryGetValue(PropertyNames.Encoding, out object annotatedEncoding))
            {
                try
                {
                    return Encoding.GetEncoding(annotatedEncoding.ToString());
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex,
                        $"Unable to determine encoding with name '{{Encoding}}'. Falling back to {{FallbackEncoding}}.",
                        annotatedEncoding.ToString(), DefaultEncoding.WebName);
                }
            }

            return DefaultEncoding;
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