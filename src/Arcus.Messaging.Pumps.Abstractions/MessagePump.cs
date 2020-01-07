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
            Logger.LogError(receiveException, "Unable to process message");
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
        protected Encoding DetermineMessageEncoding(MessageContext messageContext)
        {
            var encoding = Encoding.UTF8;
            if (messageContext.Properties.TryGetValue(PropertyNames.Encoding, out object annotatedEncoding))
                try
                {
                    encoding = Encoding.GetEncoding(annotatedEncoding.ToString());
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex,
                        $"Unable to determine encoding with name '{{Encoding}}'. Falling back to {{FallbackEncoding}}.",
                        annotatedEncoding.ToString(), encoding.WebName);
                }

            return encoding;
        }

        /// <summary>
        ///     Triggered when the message pump is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            Logger.LogWarning("Host is shutting down");
        }
    }
}