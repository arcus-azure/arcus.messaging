using System;
using Arcus.Messaging.Health.Publishing;
using Arcus.Messaging.Health.Tcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to provide dev-friendly approach to add TCP health check service.
    /// </summary>
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
        /// <param name="services">The collection of services to use in the application</param>
        /// <param name="tcpConfigurationKey">The configuration key that defines TCP health port on which the health report is exposed.</param>
        /// <param name="configureHealthChecks">The capability to configure the required health checks to expose, if required</param>
        /// <param name="configureTcpListenerOptions">The capability to configure additional options how the <see cref="TcpHealthListener"/> works, if required</param>
        /// <param name="configureHealthCheckPublisherOptions">
        ///     The capability to configure additional options regarding how fast or slow changes in the health report should affect the TCP probe's availability,
        ///     when the <see cref="TcpHealthListenerOptions.RejectTcpConnectionWhenUnhealthy"/> is set to <c>true</c>.
        /// </param>
        /// <returns>Collection of services to use in the application</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tcpConfigurationKey"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, please use the " + nameof(IHealthCheckBuilderExtensions.AddTcpHealthProbe) + " instead")]
        public static IServiceCollection AddTcpHealthProbes(
            this IServiceCollection services,
            string tcpConfigurationKey,
            Action<IHealthChecksBuilder> configureHealthChecks = null,
            Action<TcpHealthListenerOptions> configureTcpListenerOptions = null,
            Action<HealthCheckPublisherOptions> configureHealthCheckPublisherOptions = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(tcpConfigurationKey))
            {
                throw new ArgumentException("Requires a non-blank configuration key for the TCP port", nameof(tcpConfigurationKey));
            }

            IHealthChecksBuilder healthCheckBuilder = services.AddHealthChecks();
            configureHealthChecks?.Invoke(healthCheckBuilder);

            var listenerOptions = new TcpHealthListenerOptions { TcpPortConfigurationKey = tcpConfigurationKey };
            configureTcpListenerOptions?.Invoke(listenerOptions);
            services.AddSingleton(listenerOptions);

            if (listenerOptions.RejectTcpConnectionWhenUnhealthy)
            {
                services.Configure<HealthCheckPublisherOptions>(options =>
                {
                    configureHealthCheckPublisherOptions?.Invoke(options);
                });

                services.AddSingleton<IHealthCheckPublisher, TcpHealthCheckPublisher>();
            }

            services.AddSingleton(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var healthService = serviceProvider.GetRequiredService<HealthCheckService>();
                var logger = serviceProvider.GetService<ILogger<TcpHealthListener>>();
                return new TcpHealthListener(configuration, listenerOptions, healthService, logger);
            });
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<TcpHealthListener>());

            return services;
        }
    }
}