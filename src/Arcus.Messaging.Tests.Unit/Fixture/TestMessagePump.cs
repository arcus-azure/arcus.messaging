using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test <see cref="MessagePump"/> implementation to verify the message handling.
    /// </summary>
    public class TestMessagePump : MessagePump
    {
        private TestMessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger logger)
            : base(configuration, serviceProvider, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessagePump"/> class.
        /// </summary>
        /// <param name="serviceProvider">The test provider to subtract the <see cref="IMessageHandler{TMessage, TMessageContext}"/> implementations from.</param>
        public TestMessagePump(IServiceProvider serviceProvider)
            : this(new ConfigurationBuilder().Build(), serviceProvider, NullLogger.Instance)
        {
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Handle a new message that was received.
        /// </summary>
        /// <typeparam name="TMessageContext">Type of message context for the provider</typeparam>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public new async Task ProcessMessageAsync<TMessageContext>(
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
            where TMessageContext : MessageContext
        {
            await base.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
        }
    }
}