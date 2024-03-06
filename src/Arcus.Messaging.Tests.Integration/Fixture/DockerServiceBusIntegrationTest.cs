using System;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents the general setup an teardown of an integration test, using an external running Docker container to interact with.
    /// </summary>
    [Trait("Category", "Docker")]
    public abstract class DockerServiceBusIntegrationTest : IntegrationTest, IAsyncLifetime
    {
        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockerServiceBusIntegrationTest" /> class.
        /// </summary>
        /// <param name="outputWriter">The logger instance to write diagnostic messages during the interaction with Azure Service Bus instances.</param>
        protected DockerServiceBusIntegrationTest(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            var connectionString = Configuration.GetValue<string>("Arcus:Infra:ServiceBus:ConnectionString");
            var topicName = Configuration.GetValue<string>("Arcus:Infra:ServiceBus:TopicName");

            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, Logger);
        }

        /// <summary>
        /// Sends an <see cref="Order"/> message to an Azure Service Bus instance, located at the given <paramref name="connectionString"/>. 
        /// </summary>
        /// <param name="message">The Service Bus message representation of an <see cref="Order"/>.</param>
        /// <param name="connectionStringKey">The connection string key where the <paramref name="message"/> should be send to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionStringKey"/> is blank.</exception>
        public async Task SenderOrderToServiceBusAsync(ServiceBusMessage message, string connectionStringKey)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message representation of an 'Order' to send it to an Azure Service Bus instance");
            Guard.NotNullOrWhitespace(connectionStringKey, nameof(connectionStringKey), "Requires an Azure Service Bus connection string to send the 'Order' message to");
            
            var connectionString = Configuration.GetValue<string>(connectionStringKey);
            ServiceBusConnectionStringProperties serviceBusConnectionString = ServiceBusConnectionStringProperties.Parse(connectionString);

            await using (var client = new ServiceBusClient(connectionString))
            await using (ServiceBusSender messageSender = client.CreateSender(serviceBusConnectionString.EntityPath))
            {
                await messageSender.SendMessageAsync(message);
            }
        }

        /// <summary>
        /// Receives the previously send <see cref="Order"/> message as an published event on Azure Event Grid.
        /// </summary>
        /// <param name="transactionId">The transaction ID to identity the correct published <see cref="Order"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionId"/> is blank.</exception>
        /// <exception cref="XunitException">
        ///     Thrown when the received <see cref="Order"/> message event doesn't conform with the expected structure of an published <see cref="Order"/> event.
        /// </exception>
        public OrderCreatedEventData ReceiveOrderFromEventGrid(string transactionId)
        {
            Guard.NotNullOrWhitespace(transactionId, nameof(transactionId), "Requires an operation ID to uniquely identity the published 'Order' message");

            CloudEvent cloudEvent = _serviceBusEventConsumerHost.GetReceivedEvent((CloudEvent ev) =>
            {
                string json = ev.Data.ToString();
                var eventData = JsonConvert.DeserializeObject<OrderCreatedEventData>(json, new MessageCorrelationInfoJsonConverter());

                return eventData.CorrelationInfo.TransactionId == transactionId;
            }, timeout: TimeSpan.FromMinutes(1));

            var json = cloudEvent.Data.ToString();
            Assert.NotNull(json);
            var orderCreatedEventData = JsonConvert.DeserializeObject<OrderCreatedEventData>(json, new MessageCorrelationInfoJsonConverter());
            Assert.NotNull(orderCreatedEventData);

            return orderCreatedEventData;
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            await _serviceBusEventConsumerHost.StopAsync();
        }
    }
}
