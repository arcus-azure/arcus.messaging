using System;
using Arcus.Messaging.Health.Publishing;
using Arcus.Messaging.Health.Tcp;
using GuardNet;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
        public static IServiceCollection AddTcpHealthProbes(
            this IServiceCollection services,
            string tcpConfigurationKey,
            Action<IHealthChecksBuilder> configureHealthChecks = null,
            Action<TcpHealthListenerOptions> configureTcpListenerOptions = null,
            Action<HealthCheckPublisherOptions> configureHealthCheckPublisherOptions = null)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the TCP health probe to");
            Guard.NotNullOrWhitespace(tcpConfigurationKey, nameof(tcpConfigurationKey), "Requires a non-blank configuration key to retrieve the TCP port");

            IHealthChecksBuilder healthCheckBuilder = services.AddHealthChecks();
            configureHealthChecks?.Invoke(healthCheckBuilder);

            var listenerOptions = new TcpHealthListenerOptions { TcpPortConfigurationKey = tcpConfigurationKey };
            configureTcpListenerOptions?.Invoke(listenerOptions);
            
            if (listenerOptions.RejectTcpConnectionWhenUnhealthy)
            {
                services.Configure<HealthCheckPublisherOptions>(options =>
                {
                    configureHealthCheckPublisherOptions?.Invoke(options);
                });

                services.AddSingleton<IHealthCheckPublisher, TcpHealthCheckPublisher>();
            }

            services.AddSingleton(listenerOptions);
            services.AddSingleton<TcpHealthListener>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<TcpHealthListener>());

            return services;
        }
    }
}