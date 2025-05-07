using System;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Arcus.Messaging.ServiceBus.Telemetry.Serilog;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusMessageHandlerCollection"/> to use Serilog to track Azure Service bus request telemetry to Azure Application Insights.
    /// </summary>
    public static class SerilogServiceBusMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds Serilog as message correlation system to track Azure Service bus request telemetry in Azure Application Insights.
        /// </summary>
        /// <remarks>
        ///     Make sure that the <see cref="TelemetryClient"/> is available in the application services, by registering the Azure Application Insights services.
        /// </remarks>
        /// <param name="collection">The application services to add the Serilog correlation system to.</param>
        public static ServiceBusMessageHandlerCollection WithServiceBusSerilogRequestTracking(
            this ServiceBusMessageHandlerCollection collection)
        {
            return WithServiceBusSerilogRequestTracking(collection, configureOptions: null);
        }

        /// <summary>
        /// Adds Serilog as message correlation system to track Azure Service bus request telemetry in Azure Application Insights.
        /// </summary>
        /// <remarks>
        ///     Make sure that the <see cref="TelemetryClient"/> is available in the application services, by registering the Azure Application Insights services.
        /// </remarks>
        /// <param name="collection">The application services to add the Serilog correlation system to.</param>
        /// <param name="configureOptions">The additional function to manipulate the </param>
        public static ServiceBusMessageHandlerCollection WithServiceBusSerilogRequestTracking(
            this ServiceBusMessageHandlerCollection collection,
            Action<SerilogMessageCorrelationOptions> configureOptions)
        {
            collection.Services.AddSingleton<IServiceBusMessageCorrelationScope>(provider =>
            {
                var options = new SerilogMessageCorrelationOptions();
                configureOptions?.Invoke(options);

                return new SerilogServiceBusMessageCorrelationScope(
                    provider.GetRequiredService<TelemetryClient>(),
                    options,
                    provider.GetRequiredService<ILogger<SerilogServiceBusMessageCorrelationScope>>());
            });

            return collection;
        }
    }
}
