using System.Threading.Tasks;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    [Trait("Category", "Docker")]
    public abstract class DockerServiceBusIntegrationTest : IntegrationTest, IAsyncLifetime
    {
        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockerServiceBusIntegrationTest" /> class.
        /// </summary>
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

        public async Task SenderOrderToServiceBusAsync(Message message, string connectionString)
        {
            MessageSender sender = CreateServiceBusSender(connectionString);
            await sender.SendAsync(message);
        }
        
        private MessageSender CreateServiceBusSender(string connectionStringKey)
        {
            var connectionString = Configuration.GetValue<string>(connectionStringKey);
            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            var messageSender = new MessageSender(serviceBusConnectionStringBuilder);
            return messageSender;
        }

        public OrderCreatedEventData ReceiveOrderFromEventGrid(string operationId)
        {
            string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId);
            Assert.NotEmpty(receivedEvent);
            var deserializedEventGridMessage = EventParser.Parse(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            var orderCreatedEvent = Assert.Single(deserializedEventGridMessage.Events);
            Assert.NotNull(orderCreatedEvent);
            var orderCreatedEventData = orderCreatedEvent.GetPayload<OrderCreatedEventData>();
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
