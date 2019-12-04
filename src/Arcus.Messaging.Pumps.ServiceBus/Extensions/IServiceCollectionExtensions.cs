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
        public static IServiceCollection AddServiceBusMessageHandler<TMessageHandler>(this IServiceCollection services)
            where TMessageHandler : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            services.AddHostedService<TMessageHandler>();

            return services;
        }
    }
}