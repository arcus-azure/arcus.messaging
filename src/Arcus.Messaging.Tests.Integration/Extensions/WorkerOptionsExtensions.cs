using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Azure;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.Azure;
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
        /// Adds an <see cref="EventGridPublisherClient"/> instance to the <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The options to add the publisher to.</param>
        /// <param name="config">The test configuration which will be used to retrieve the Azure Event Grid authentication information.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or the <paramref name="config"/> is <c>null</c>.</exception>
        public static WorkerOptions AddEventGridPublisher(this WorkerOptions options, TestConfig config)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of worker options to add the Azure Event Grid publisher to");
            Guard.NotNull(config, nameof(config), "Requires a test configuration instance to retrieve the Azure Event Grid authentication inforation");
            
            options.Services.AddAzureClients(clients =>
            {
                string topicEndpoint = config.GetTestInfraEventGridTopicUri();
                string authenticationKey = config.GetTestInfraEventGridAuthKey();
                clients.AddEventGridPublisherClient(new Uri(topicEndpoint), new AzureKeyCredential(authenticationKey));
            });

            return options;
        }

        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(this WorkerOptions options, string connectionString)
        {
            return options.Services.AddServiceBusTopicMessagePump(subscriptionName: Guid.NewGuid().ToString(), _ => connectionString, opt =>
            {
                opt.TopicSubscription = TopicSubscription.Automatic;
                opt.AutoComplete = true;
            });
        }
    }
}
