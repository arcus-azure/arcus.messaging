using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a message pump that processes messages from an Azure Service Bus entity that supports sessions.
    /// </summary>
    internal class ServiceBusSessionMessagePump : ServiceBusMessagePump
    {
        private readonly Func<IServiceProvider, ServiceBusClient> _clientImplementationFactory;
        private ServiceBusSessionProcessor _sessionProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusSessionMessagePump"/> class.
        /// </summary>
        internal ServiceBusSessionMessagePump(
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            ServiceBusEntityType entityType,
            string entityName,
            string subscriptionName,
            IServiceProvider serviceProvider,
            ServiceBusMessagePumpOptions options,
            ILogger logger)
            : base(entityType, entityName, subscriptionName, serviceProvider, options, logger)
        {
            _clientImplementationFactory = clientImplementationFactory;
        }

        /// <summary>
        /// Gets the namespace of the Azure Service Bus entity that this message pump is processing messages for.
        /// </summary>
        protected override string Namespace => _sessionProcessor?.FullyQualifiedNamespace;

        /// <summary>
        /// Sets up the message pump to start processing messages from the Azure Service Bus entity.
        /// </summary>
        protected override async Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            _sessionProcessor = CreateSessionProcessor();

            _sessionProcessor.ProcessMessageAsync += ProcessMessageAsync;
            _sessionProcessor.ProcessErrorAsync += ProcessErrorAsync;

            await _sessionProcessor.StartProcessingAsync(cancellationToken);
        }

        private ServiceBusSessionProcessor CreateSessionProcessor()
        {
            ServiceBusClient client = _clientImplementationFactory(ServiceProvider);
            var clientOptions = new ServiceBusSessionProcessorOptions
            {
                PrefetchCount = Options.PrefetchCount,
                MaxConcurrentSessions = Options.Session.MaxConcurrentSessions,
                MaxConcurrentCallsPerSession = Options.Session.MaxConcurrentCallsPerSession,
                SessionIdleTimeout = Options.Session.SessionIdleTimeout,
            };

            return string.IsNullOrWhiteSpace(SubscriptionName)
                ? client.CreateSessionProcessor(EntityName, clientOptions)
                : client.CreateSessionProcessor(EntityName, SubscriptionName, clientOptions);
        }

        private async Task ProcessMessageAsync(ProcessSessionMessageEventArgs arg)
        {
            ServiceBusReceivedMessage message = arg?.Message;
            if (message is null)
            {
                Logger.LogWarning("Received message on Azure Service Bus {EntityType} session-aware message pump '{JobId}' was null, skipping", EntityType, JobId);
                return;
            }

            var messageContext = ServiceBusMessageContext.Create(JobId, EntityType, SubscriptionName, arg);
            await RouteMessageAsync(message, messageContext, arg.CancellationToken);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            if (arg.Exception is null)
            {
                Logger.LogWarning("Thrown exception on Azure Service Bus {EntityType} session-aware message pump '{JobId}' was null, skipping", EntityType, JobId);
                return Task.CompletedTask;
            }

            Logger.LogCritical(arg.Exception, "Azure Service Bus session-aware message pump '{JobId}' was unable to process message: {Message}", JobId, arg.Exception.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        /// <returns>A <see cref="Task" /> that represents the asynchronous Stop operation.</returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_sessionProcessor != null)
            {
                await _sessionProcessor.StopProcessingAsync(cancellationToken);
                await _sessionProcessor.CloseAsync(cancellationToken);
            }

            await base.StopAsync(cancellationToken);
        }
    }
}