using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.EventHubs.Configuration;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Testing;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using TestConfig = Arcus.Messaging.Tests.Integration.Fixture.TestConfig;
using XunitTestLogger = Arcus.Testing.Logging.XunitTestLogger;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public partial class EventHubsMessagePumpTests : IAsyncLifetime
    {
        private readonly TestConfig _config;
        private readonly EventHubsConfig _eventHubsConfig;
        private readonly ILogger _logger;
        private readonly ITestOutputHelper _outputWriter;

        private TemporaryBlobContainer _blobStorageContainer;
        private TemporaryManagedIdentityConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsMessagePumpTests" /> class.
        /// </summary>
        public EventHubsMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _logger = new XunitTestLogger(outputWriter);
            
            _config = TestConfig.Create();
            _eventHubsConfig = _config.GetEventHubsConfig();
        }

        private string EventHubsName => _eventHubsConfig.GetEventHubsName(IntegrationTestType.SelfContained);
        private string FullyQualifiedEventHubsNamespace => EventHubsConnectionStringProperties.Parse(_eventHubsConfig.EventHubsConnectionString).FullyQualifiedNamespace;
        private string ContainerName => _blobStorageContainer.Name;

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _connection = TemporaryManagedIdentityConnection.Create(_config, _logger);
            _blobStorageContainer = await TemporaryBlobContainer.CreateIfNotExistsAsync(_eventHubsConfig.Storage.Name, $"test-{Guid.NewGuid()}", _logger);
        }

        private EventHubsMessageHandlerCollection AddEventHubsMessagePump(WorkerOptions options, Action<AzureEventHubsMessagePumpOptions> configureOptions = null)
        {
            return options.AddXunitTestLogging(_outputWriter)
                          .AddEventHubsMessagePumpUsingManagedIdentity(
                              eventHubsName: EventHubsName,
                              fullyQualifiedNamespace: FullyQualifiedEventHubsNamespace,
                              blobContainerUri: _blobStorageContainer.Client.Uri.ToString(),
                              clientId: _connection.ClientId,
                              configureOptions);
        }

        private async Task TestEventHubsMessageHandlingAsync(
            Action<WorkerOptions> configureOptions, 
            MessageCorrelationFormat format = MessageCorrelationFormat.W3C,
            [CallerMemberName] string memberName = null)
        {
            EventData message = format switch
            {
                MessageCorrelationFormat.W3C => CreateSensorEventDataForW3C(),
                MessageCorrelationFormat.Hierarchical => CreateSensorEventDataForHierarchical(),
            };

            var options = new WorkerOptions();
            configureOptions(options);

            await TestEventHubsMessageHandlingAsync(options, message, async () =>
            {
                SensorReadEventData eventData = await DiskMessageEventConsumer.ConsumeSensorReadAsync(message.MessageId);
                switch (format)
                {
                    case MessageCorrelationFormat.W3C:
                        AssertReceivedSensorEventDataForW3C(message, eventData);
                        break;
                
                    case MessageCorrelationFormat.Hierarchical:
                        AssertReceivedSensorEventDataForHierarchical(message, eventData);
                        break;
                
                    default:
                        throw new ArgumentOutOfRangeException(nameof(format), format, null);
                }
            }, memberName);
        }

        private async Task TestEventHubsMessageHandlingAsync(
            WorkerOptions options,
            EventData message,
            Func<Task> assertionAsync,
            [CallerMemberName] string memberName = null)
        {
            // Arrange
            options.AddXunitTestLogging(_outputWriter);

            await using var worker = await Worker.StartNewAsync(options, memberName);
            TestEventHubsMessageProducer producer = CreateEventHubsMessageProducer();

            // Act
            await producer.ProduceAsync(message);

            // Assert
            await assertionAsync();
        }

        private static EventData CreateSensorEventDataForHierarchical(
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId,
            Encoding encoding = null)
        {
            var operationId = $"operation-{Guid.NewGuid()}";
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            SensorReading reading = SensorReadingGenerator.Generate();
            EventData message =
                EventDataBuilder.CreateForBody(reading, encoding ?? Encoding.UTF8)
                                .WithOperationId(operationId)
                                .WithTransactionId(transactionId, transactionIdPropertyName)
                                .WithOperationParentId(operationParentId, operationParentIdPropertyName)
                                .Build();

            message.MessageId = reading.SensorId;
            return message;
        }

        private static EventData CreateSensorEventDataForW3C(Encoding encoding = null, TraceParent traceParent = null)
        {
            encoding ??= Encoding.UTF8;
            traceParent ??= TraceParent.Generate();

            SensorReading reading = SensorReadingGenerator.Generate();
            string json = JsonConvert.SerializeObject(reading);
            byte[] raw = encoding.GetBytes(json);

            var message = new EventData(raw)
            {
                MessageId = reading.SensorId,
                Properties =
                {
                    { PropertyNames.ContentType, "application/json" },
                    { PropertyNames.Encoding, encoding.WebName }
                }
            };

            return message.WithDiagnosticId(traceParent);
        }

        private static void AssertReceivedSensorEventDataForHierarchical(
            EventData message,
            SensorReadEventData receivedEventData,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId,
            Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            string json = encoding.GetString(message.EventBody.ToArray());

            var reading = JsonConvert.DeserializeObject<SensorReading>(json);
            string operationId = message.CorrelationId;
            var transactionId = message.Properties[transactionIdPropertyName].ToString();
            var operationParentId = message.Properties[operationParentIdPropertyName].ToString();

            Assert.NotNull(receivedEventData);
            Assert.NotNull(receivedEventData.CorrelationInfo);
            Assert.Equal(reading.SensorId, receivedEventData.SensorId);
            Assert.Equal(reading.SensorValue, receivedEventData.SensorValue);
            Assert.Equal(transactionId, receivedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, receivedEventData.CorrelationInfo.OperationId);
            Assert.Equal(operationParentId, receivedEventData.CorrelationInfo.OperationParentId);
        }

        private static void AssertReceivedSensorEventDataForW3C(
            EventData message,
            SensorReadEventData receivedEventData)
        {
            var encoding = Encoding.GetEncoding(message.Properties[PropertyNames.Encoding].ToString() ?? Encoding.UTF8.WebName);
            string json = encoding.GetString(message.EventBody.ToArray());

            var reading = JsonConvert.DeserializeObject<SensorReading>(json);

            (string transactionId, string operationParentId) = message.Properties.GetTraceParent();
            Assert.NotNull(receivedEventData);
            Assert.NotNull(receivedEventData.CorrelationInfo);
            Assert.Equal(reading.SensorId, receivedEventData.SensorId);
            Assert.Equal(reading.SensorValue, receivedEventData.SensorValue);
            Assert.Equal(transactionId, receivedEventData.CorrelationInfo.TransactionId);
            Assert.NotNull(receivedEventData.CorrelationInfo.OperationId);
            Assert.Equal(operationParentId, receivedEventData.CorrelationInfo.OperationParentId);
        }

        private TestEventHubsMessageProducer CreateEventHubsMessageProducer()
        {
            return new TestEventHubsMessageProducer(
                _eventHubsConfig.EventHubsConnectionString,
                EventHubsName);
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_blobStorageContainer != null)
            {
                await _blobStorageContainer.DisposeAsync();
            }

            _connection?.Dispose();
        }
    }
}
