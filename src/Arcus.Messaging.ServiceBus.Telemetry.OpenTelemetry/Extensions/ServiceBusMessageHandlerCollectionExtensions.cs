using System;
using System.Diagnostics;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Arcus.Messaging.ServiceBus.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusMessageHandlerCollection"/> to register OpenTelemetry services for Azure Service Bus message pumps.
    /// </summary>
    public static class ServiceBusMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Register OpenTelemetry as the correlation system to track Azure Service Bus message requests within the message pump.
        /// </summary>
        /// <param name="handlers">The collection of Azure Service Bus message handler collection.</param>
        /// <param name="activitySource">The activity source to start <see cref="Activity"/> instances from upon receiving Azure Service Bus messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handlers"/> or the <paramref name="activitySource"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection UseServiceBusOpenTelemetryRequestTracking(
            this ServiceBusMessageHandlerCollection handlers,
            ActivitySource activitySource)
        {
            ArgumentNullException.ThrowIfNull(handlers);
            ArgumentNullException.ThrowIfNull(activitySource);

            handlers.Services.TryAddSingleton<IServiceBusMessageCorrelationScope>(new OpenTelemetryServiceBusMessageCorrelationScope(activitySource));
            return handlers;
        }
    }
}
