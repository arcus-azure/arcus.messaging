using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Extra test-specific extensions on the <see cref="WorkerOptions"/>.
    /// </summary>
    public static class WorkerOptionsExtensions
    {
         /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace.
        /// </remarks>
        /// <param name="options">The collection of services to add the message pump to.</param>
        /// <param name="connectionString">The connection string scoped to the Azure Service Bus Topic from the configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> is blank.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(this WorkerOptions options, string connectionString)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            return options.Services.AddServiceBusTopicMessagePump(
                subscriptionName: Guid.NewGuid().ToString(),
                _ => connectionString, opt =>
                {
                    opt.TopicSubscription = TopicSubscription.Automatic;
                    opt.AutoComplete = true;
                });
        }

        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpUsingManagedIdentity(
            this WorkerOptions options,
            string topicName,
            string hostName)
        {
            return options.Services.AddServiceBusTopicMessagePumpUsingManagedIdentity(
                topicName,
                subscriptionName: Guid.NewGuid().ToString(),
                serviceBusNamespace: hostName,
                configureMessagePump: opt =>
                {
                    opt.TopicSubscription = TopicSubscription.Automatic;
                    opt.AutoComplete = true;
                });
        }
    }
}
