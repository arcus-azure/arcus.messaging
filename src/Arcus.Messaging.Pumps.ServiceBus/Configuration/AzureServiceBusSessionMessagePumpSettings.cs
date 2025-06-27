using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    internal class AzureServiceBusSessionMessagePumpSettings
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly Func<IServiceProvider, Task<(ServiceBusClient client, string entityPath)>> _clientImplementationFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpSettings"/> class.
        /// </summary>
        /// <param name="entityName">The name of the entity to process.</param>
        /// <param name="subscriptionName">The name of the subscription to process.</param>
        /// <param name="serviceBusEntity">The entity type of the Azure Service Bus.</param>
        /// <param name="clientImplementationFactory">The function to look up the connection string from the configuration.</param>
        /// <param name="options">The options that influence the behavior of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <param name="serviceProvider">The collection of services to use during the lifetime of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="clientImplementationFactory"/>, <paramref name="options"/> or <paramref name="serviceProvider"/> is <c>null</c>.
        /// </exception>
        internal AzureServiceBusSessionMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            AzureServiceBusSessionMessagePumpOptions options,
            IServiceProvider serviceProvider)
        {
            if (serviceBusEntity is ServiceBusEntityType.Topic && string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription name", nameof(subscriptionName));
            }

            if (clientImplementationFactory is null)
            {
                throw new ArgumentNullException(nameof(clientImplementationFactory));
            }

            if (!Enum.IsDefined(typeof(ServiceBusEntityType), serviceBusEntity) || serviceBusEntity is ServiceBusEntityType.Unknown)
            {
                throw new ArgumentException(
                    $"Azure Service Bus entity type should either be '{ServiceBusEntityType.Queue}' or '{ServiceBusEntityType.Topic}'", nameof(serviceBusEntity));
            }

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _clientImplementationFactory = provider =>
            {
                ServiceBusClient client = clientImplementationFactory(provider);
                return Task.FromResult((client, entityName));
            };

            EntityName = entityName;
            SubscriptionName = SanitizeSubscriptionName(subscriptionName, serviceProvider);
            ServiceBusEntity = serviceBusEntity;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private static string SanitizeSubscriptionName(string subscriptionName, IServiceProvider provider)
        {
            var logger =
                provider.GetService<ILogger<AzureServiceBusMessagePump>>()
                ?? NullLogger<AzureServiceBusMessagePump>.Instance;

            if (subscriptionName != null && subscriptionName.Length > 50)
            {
                logger.LogWarning("Azure Service Bus Topic subscription name was truncated to 50 characters");
                subscriptionName = subscriptionName.Substring(0, 50);
            }

            return subscriptionName;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus entity to process.
        /// </summary>
        /// <remarks>This is optional as the connection string can contain the entity name</remarks>
        public string EntityName { get; private set; }

        /// <summary>
        /// Gets the name of the Azure Service Bus Topic subscription.
        /// </summary>
        /// <remarks>This is only applicable when using Azure Service Bus Topics</remarks>
        public string SubscriptionName { get; }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity.
        /// </summary>
        public ServiceBusEntityType ServiceBusEntity { get; }

        /// <summary>
        /// Gets the additional options that influence the behavior of the message pump.
        /// </summary>
        public AzureServiceBusSessionMessagePumpOptions Options { get; }

        /// <summary>
        /// Creates an <see cref="ServiceBusReceiver"/> instance based on the provided settings.
        /// </summary>
        internal async Task<ServiceBusSessionProcessor> CreateSessionProcessorAsync()
        {
            (ServiceBusClient client, string entityPath) = await _clientImplementationFactory(_serviceProvider);
            EntityName = entityPath;

            var options = new ServiceBusSessionProcessorOptions
            {
                PrefetchCount = Options.PrefetchCount,
                MaxConcurrentCallsPerSession = Options.MaxConcurrentCallsPerSession,
                MaxConcurrentSessions = Options.MaxConcurrentSessions,
                SessionIdleTimeout = Options.SessionIdleTimeout
            };

            return string.IsNullOrWhiteSpace(SubscriptionName)
                ? client.CreateSessionProcessor(EntityName, options)
                : client.CreateSessionProcessor(EntityName, SubscriptionName, options);
        }
    }
}
