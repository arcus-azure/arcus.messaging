using System;
using Arcus.Messaging.Pumps.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(this IServiceCollection services, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, configureMessagePump);

            return services;
        }

        private static void AddServiceBusMessagePump<TMessagePump>(IServiceCollection services, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            var messagePumpOptions = AzureServiceBusMessagePumpOptions.Default;
            configureMessagePump?.Invoke(messagePumpOptions);

            services.AddTransient(serviceProvider => messagePumpOptions);
            services.AddHostedService<TMessagePump>();
        }
    }
}