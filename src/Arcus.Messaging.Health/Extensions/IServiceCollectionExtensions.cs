using System;
using Arcus.Messaging.Health.Tcp;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds TCP health probing
        /// </summary>
        /// <remarks>
        ///     In order for the TCP health probes to work, ASP.NET Core Health Checks must be configured. You can configure
        ///     it here or do it yourself as well.
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="tcpConfigurationKey">The TCP health port on which the health report is exposed.</param>
        /// <param name="configureHealthChecks">Capability to configure the required health checks to expose, if required</param>
        /// <param name="configureTcpListenerOptions">Capability to configure additional options how the <see cref="TcpHealthListener"/> works, if required</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddTcpHealthProbes(
            this IServiceCollection services,
            string tcpConfigurationKey,
            Action<IHealthChecksBuilder> configureHealthChecks = null,
            Action<TcpHealthListenerOptions> configureTcpListenerOptions = null)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the TCP health probe to");
            Guard.NotNullOrWhitespace(tcpConfigurationKey, nameof(tcpConfigurationKey), "Requires a non-blank configuration key to retrieve the TCP port");

            var healthCheckBuilder = services.AddHealthChecks();
            configureHealthChecks?.Invoke(healthCheckBuilder);

            services.Configure<TcpHealthListenerOptions>(options =>
            {
                options.TcpPortConfigurationKey = tcpConfigurationKey;
                configureTcpListenerOptions?.Invoke(options);
            });

            services.AddHostedService<TcpHealthListener>();

            return services;
        }
    }
}