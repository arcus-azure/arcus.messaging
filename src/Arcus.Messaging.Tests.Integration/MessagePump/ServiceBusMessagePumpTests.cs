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
            ServiceBusEntityType registeredEntityType =
                Assert.Single(
                    worker.Services.GetServices<IHostedService>()
                                   .Where(h => h is AzureServiceBusMessagePump)
                                   .Cast<AzureServiceBusMessagePump>()
                                   .Select(m => m.EntityType)
                                   .ToArray());

            return registeredEntityType;
        }

        private static ServiceBusMessage CreateOrderServiceBusMessageForW3C(Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            TraceParent traceParent = TraceParent.Generate();

            Order order = OrderGenerator.Generate();
            string json = JsonConvert.SerializeObject(order);
            byte[] raw = encoding.GetBytes(json);

            var message = new ServiceBusMessage(raw)
            {
                MessageId = order.Id,
                ApplicationProperties =
                {
                    { PropertyNames.Encoding, encoding.WebName }
                }
            };

            message.ApplicationProperties["Diagnostic-Id"] = traceParent.DiagnosticId;
            return message;
        }

        private static void AssertReceivedOrderEventDataForW3C(
            ServiceBusMessage message,
            OrderCreatedEventData receivedEventData)
        {
            var encoding = Encoding.GetEncoding(message.ApplicationProperties[PropertyNames.Encoding].ToString() ?? Encoding.UTF8.WebName);
            string json = encoding.GetString(message.Body);

            var order = JsonConvert.DeserializeObject<Order>(json);

            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();
            Assert.NotNull(receivedEventData);
            Assert.NotNull(receivedEventData.CorrelationInfo);
            Assert.Equal(order.Id, receivedEventData.Id);
            Assert.Equal(order.Amount, receivedEventData.Amount);
            Assert.Equal(order.ArticleNumber, receivedEventData.ArticleNumber);
            Assert.Equal(transactionId, receivedEventData.CorrelationInfo.TransactionId);
            Assert.NotNull(receivedEventData.CorrelationInfo.OperationId);
            Assert.Equal(operationParentId, receivedEventData.CorrelationInfo.OperationParentId);
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