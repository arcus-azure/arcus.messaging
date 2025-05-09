using System;
using Arcus.Messaging.Health.Publishing;
using Arcus.Messaging.Health.Tcp;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IHealthChecksBuilder"/> to provide dev-friendly approach to add TCP health check service.
    /// </summary>
    public static class IHealthCheckBuilderExtensions
    {
        /// <summary>
        /// Adds TCP probe that exposes the application's health status over a <paramref name="tcpPort"/>.
        /// </summary>
        /// <param name="builder">The health check builder to register the TCP probe.</param>
        /// <param name="tcpPort">The TCP port where the health report is exposed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="tcpPort"/> is zero or below.</exception>
        public static IHealthChecksBuilder AddTcpHealthProbe(this IHealthChecksBuilder builder, int tcpPort)
        {
            return AddTcpHealthProbe(builder, tcpPort, configureOptions: null);
        }

        /// <summary>
        /// Adds TCP probe that exposes the application's health status over a <paramref name="tcpPort"/>.
        /// </summary>
        /// <remarks>
        ///     When the <see cref="TcpHealthListenerOptions.RejectTcpConnectionWhenUnhealthy"/> is enabled,
        ///     the TCP health probe makes use of the <see cref="IHealthCheckPublisher"/> functionality.
        /// </remarks>
        /// <param name="builder">The health check builder to register the TCP probe.</param>
        /// <param name="tcpPort">The TCP port where the health report is exposed.</param>
        /// <param name="configureOptions">The function to configure the options that manipulate the behavior of the TCP probe.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="tcpPort"/> is zero or below.</exception>
        public static IHealthChecksBuilder AddTcpHealthProbe(this IHealthChecksBuilder builder, int tcpPort, Action<TcpHealthListenerOptions> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (tcpPort <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tcpPort), "Requires a TCP port that is greater than zero");
            }

            var options = new TcpHealthListenerOptions();
            configureOptions?.Invoke(options);

            if (options.RejectTcpConnectionWhenUnhealthy)
            {
                builder.Services.AddSingleton<IHealthCheckPublisher>(serviceProvider =>
                {
                    var listener = serviceProvider.GetRequiredService<TcpHealthListener>();
                    var logger = serviceProvider.GetService<ILogger<TcpHealthCheckPublisher>>();
                    return new TcpHealthCheckPublisher(listener, options, logger);
                });
            }

            builder.Services.AddSingleton(serviceProvider =>
            {
                var healthService = serviceProvider.GetRequiredService<HealthCheckService>();
                var logger = serviceProvider.GetService<ILogger<TcpHealthListener>>();
                return new TcpHealthListener(tcpPort, healthService, options, logger);
            });
            builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<TcpHealthListener>());

            return builder;
        }
    }
}