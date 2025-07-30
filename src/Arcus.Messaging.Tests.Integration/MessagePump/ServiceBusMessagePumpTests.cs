using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public partial class ServiceBusMessagePumpTests : IntegrationTest, IClassFixture<TemporaryServiceBusEntityState>, IDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly TemporaryServiceBusEntityState _entity;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusMessagePumpTests(TemporaryServiceBusEntityState entity, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _entity = entity;
            _connection = TemporaryManagedIdentityConnection.Create(Configuration, Logger);
        }

        [Fact(Skip = ".NET application cannot start multiple blocking background tasks, see https://github.com/dotnet/runtime/issues/36063" +
                     "will maybe be in the .NET 10 release in November")]
        public async Task ServiceBusMessagePumpWithQueueAndTopic_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithMatchedServiceBusMessageHandler();

            serviceBus.WhenServiceBusTopicMessagePump()
                      .WithMatchedServiceBusMessageHandler();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.GivenServiceBus(_entity, Logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }

    public class TemporaryServiceBusEntityState : IAsyncLifetime
    {
        private ServiceBusClient _client;

        public TemporaryQueue Queue { get; set; }
        public TemporaryQueue QueueWithSession { get; set; }
        public TemporaryTopic Topic { get; set; }
        public string QueueName { get; } = $"queue-{Guid.NewGuid()}";
        public string TopicName { get; } = $"topic-{Guid.NewGuid()}";
        public ServiceBusConfig ServiceBusConfig { get; private set; }

        public async ValueTask InitializeAsync()
        {
            ServiceBusConfig = TestConfig.Create().GetServiceBus();
            ServiceBusAdministrationClient adminClient = ServiceBusConfig.GetAdminClient();

            _client = ServiceBusConfig.GetClient();
            Topic = await TemporaryTopic.CreateIfNotExistsAsync(adminClient, _client, TopicName, NullLogger.Instance, temp => temp.OnTeardown.CompleteMessages());
            Queue = await TemporaryQueue.CreateIfNotExistsAsync(adminClient, _client, QueueName, NullLogger.Instance, temp =>
            {
                temp.OnTeardown.CompleteMessages();
            });

            QueueWithSession = await TemporaryQueue.CreateIfNotExistsAsync(adminClient, _client, $"{QueueName}-session", NullLogger.Instance, temp =>
            {
                temp.OnSetup.CreateQueueWith(queue => queue.RequiresSession = true);
                temp.OnTeardown.CompleteMessages();
            });
        }

        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(NullLogger.Instance);
            disposables.Add(Queue);
            disposables.Add(Topic);
            disposables.Add(_client);
        }
    }
}