using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Extensions.Logging.ServiceBusEntityType;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public partial class ServiceBusMessagePumpTests : IClassFixture<ServiceBusEntityFixture>
    {
        private readonly TestConfig _config;
        private readonly ServiceBusConfig _serviceBusConfig;
        private readonly ILogger _logger;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusMessagePumpTests(ServiceBusEntityFixture entity, ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _serviceBusConfig = _config.GetServiceBus();

            _outputWriter = outputWriter;
            _logger = new XunitTestLogger(outputWriter);

            QueueName = entity.QueueName;
            TopicName = entity.TopicName;
        }

        private string QueueName { get; }
        private string TopicName { get; }
        private string HostName => _serviceBusConfig.HostName;
        private string NamespaceConnectionString => _serviceBusConfig.NamespaceConnectionString;
        private string QueueConnectionString => $"{_serviceBusConfig.NamespaceConnectionString};EntityPath={QueueName}";
        private string TopicConnectionString => $"{_serviceBusConfig.NamespaceConnectionString};EntityPath={TopicName}";

        [Fact(Skip = ".NET application cannot start multiple blocking background tasks, see https://github.com/dotnet/runtime/issues/36063")]
        public async Task ServiceBusMessagePumpWithQueueAndTopic_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            options.AddServiceBusTopicMessagePumpUsingManagedIdentity(TopicName, HostName)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, Queue);
            await TestServiceBusMessageHandlingAsync(options, Topic);
        }

        private async Task TestServiceBusMessageHandlingAsync(
            WorkerOptions options, 
            ServiceBusEntityType entityType, 
            [CallerMemberName] string memberName = null)
        {
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            await TestServiceBusMessageHandlingAsync(options, entityType, message, async () =>
            {
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData);
            }, memberName);
        }

        private async Task TestServiceBusMessageHandlingAsync(
            ServiceBusEntityType entityType, 
            Action<WorkerOptions> configureOptions, 
            MessageCorrelationFormat format = MessageCorrelationFormat.W3C,
            [CallerMemberName] string memberName = null)
        {
            ServiceBusMessage message = format switch
            {
                MessageCorrelationFormat.W3C => CreateOrderServiceBusMessageForW3C(),
                MessageCorrelationFormat.Hierarchical => CreateOrderServiceBusMessageForHierarchical(),
            };

            using var connection = TemporaryManagedIdentityConnection.Create(_config, _logger);

            var options = new WorkerOptions();
            configureOptions(options);

            await TestServiceBusMessageHandlingAsync(options, entityType, message, async () =>
            {
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                switch (format)
                {
                    case MessageCorrelationFormat.W3C:
                        AssertReceivedOrderEventDataForW3C(message, eventData);
                        break;
                
                    case MessageCorrelationFormat.Hierarchical:
                        AssertReceivedOrderEventDataForHierarchical(message, eventData);
                        break;
                
                    default:
                        throw new ArgumentOutOfRangeException(nameof(format), format, null);
                }
            }, memberName);
        }

        private async Task TestServiceBusMessageHandlingAsync(
            WorkerOptions options,
            ServiceBusEntityType entityType,
            ServiceBusMessage message,
            Func<Task> assertionAsync,
            [CallerMemberName] string memberName = null)
        {
            // Arrange
            options.AddXunitTestLogging(_outputWriter);

            await using var worker = await Worker.StartNewAsync(options, memberName);
            ServiceBusEntityType registeredEntityType = DetermineRegisteredEntityType(worker);

            var producer = TestServiceBusMessageProducer.CreateFor(registeredEntityType switch
            {
                Queue => QueueName,
                Topic => TopicName,
                _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service bus entity type")
            }, _config);

            // Act
            await producer.ProduceAsync(message);

            // Assert
            await assertionAsync();
        }

        private static ServiceBusEntityType DetermineRegisteredEntityType(Worker worker)
        {
            ServiceBusEntityType registeredEntityType =
                Assert.Single(
                    worker.Services.GetServices<IHostedService>()
                                   .Where(h => h is AzureServiceBusMessagePump)
                                   .Cast<AzureServiceBusMessagePump>()
                                   .Select(m => m.Settings.ServiceBusEntity)
                                   .ToArray());

            return registeredEntityType;
        }

        private static ServiceBusMessage CreateOrderServiceBusMessageForHierarchical(
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId,
            Encoding encoding = null)
        {
            var operationId = $"operation-{Guid.NewGuid()}";
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            Order order = OrderGenerator.Generate();
            ServiceBusMessage message =
                ServiceBusMessageBuilder.CreateForBody(order, encoding ?? Encoding.UTF8)
                                        .WithOperationId(operationId)
                                        .WithTransactionId(transactionId, transactionIdPropertyName)
                                        .WithOperationParentId(operationParentId, operationParentIdPropertyName)
                                        .Build();

            message.MessageId = order.Id;
            return message;
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
                    { PropertyNames.ContentType, "application/json" },
                    { PropertyNames.Encoding, encoding.WebName }
                }
            };

            return message.WithDiagnosticId(traceParent);
        }

        private static void AssertReceivedOrderEventDataForHierarchical(
            ServiceBusMessage message,
            OrderCreatedEventData receivedEventData,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId,
            Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            string json = encoding.GetString(message.Body);

            var order = JsonConvert.DeserializeObject<Order>(json);
            string operationId = message.CorrelationId;
            var transactionId = message.ApplicationProperties[transactionIdPropertyName].ToString();
            var operationParentId = message.ApplicationProperties[operationParentIdPropertyName].ToString();

            Assert.NotNull(receivedEventData);
            Assert.NotNull(receivedEventData.CorrelationInfo);
            Assert.Equal(order.Id, receivedEventData.Id);
            Assert.Equal(order.Amount, receivedEventData.Amount);
            Assert.Equal(order.ArticleNumber, receivedEventData.ArticleNumber);
            Assert.Equal(transactionId, receivedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, receivedEventData.CorrelationInfo.OperationId);
            Assert.Equal(operationParentId, receivedEventData.CorrelationInfo.OperationParentId);
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
    }

    public class ServiceBusEntityFixture : IAsyncLifetime
    {
        private TemporaryServiceBusEntity _queue, _topic;

        public string QueueName { get; } = $"queue-{Guid.NewGuid()}";
        public string TopicName { get; } = $"topic-{Guid.NewGuid()}";

        public async Task InitializeAsync()
        {
            var config = TestConfig.Create().GetServiceBus();
            _topic = await TemporaryServiceBusEntity.CreateAsync(Topic, TopicName, config, NullLogger.Instance);
            _queue = await TemporaryServiceBusEntity.CreateAsync(Queue, QueueName, config, NullLogger.Instance);
        }

        public async Task DisposeAsync()
        {
            await using var disposables = new DisposableCollection(NullLogger.Instance);
            disposables.Add(_queue);
            disposables.Add(_topic);
        }
    }
}