using GuardNet;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus health probing
        /// </summary>
        /// <param name="services">Collection of services to use in the application</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(this IServiceCollection services)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            services.AddHostedService<TMessagePump>();

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus health probing
        /// </summary>
        /// <param name="services">Collection of services to use in the application</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            services.AddHostedService<TMessagePump>();

            return services;
        }
    }
}