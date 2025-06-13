using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Extra test-specific extensions on the <see cref="WorkerOptions"/>.
    /// </summary>
    public static class WorkerOptionsExtensions
    {
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpUsingManagedIdentity(
            this WorkerOptions options,
            string topicName,
            string hostName)
        {
            return options.Services.AddServiceBusTopicMessagePump(
                topicName,
                subscriptionName: Guid.NewGuid().ToString(),
                fullyQualifiedNamespace: hostName,
                new DefaultAzureCredential(),
                configureMessagePump: opt =>
                {
                    opt.TopicSubscription = TopicSubscription.Automatic;
                    opt.AutoComplete = true;
                });
        }
    }
}
