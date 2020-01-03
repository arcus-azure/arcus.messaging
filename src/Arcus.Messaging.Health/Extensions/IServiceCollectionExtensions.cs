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
        /// <param name="configureHealthChecks">Capability to configure the required health checks to expose, if required</param>
        /// <param name="configureTcpListenerOptions">Capability to configure additional options how the <see cref="TcpHealthListener"/> works, if required</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddTcpHealthProbes(
            this IServiceCollection services,
            Action<IHealthChecksBuilder> configureHealthChecks = null,
            Action<TcpHealthListenerOptions> configureTcpListenerOptions = null)
        {
            Guard.NotNull(services, nameof(services));

            var healthCheckBuilder = services.AddHealthChecks();
            configureHealthChecks?.Invoke(healthCheckBuilder);

            if (configureTcpListenerOptions != null)
            {
                services.Configure(configureTcpListenerOptions);
            }

            services.AddHostedService<TcpHealthListener>();

            return services;
        }
    }
}