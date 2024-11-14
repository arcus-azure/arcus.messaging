using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump;
using Arcus.Testing;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.EventHubs
{
    public class AzureClientFactoryBuilderExtensionsTests : IClassFixture<EventHubsEntityFixture>, IAsyncLifetime
    {
        private readonly TestConfig _config;
        private readonly EventHubsConfig _eventHubsConfig;
        private readonly ILogger _logger;

        private TemporaryManagedIdentityConnection _connection;
        private TemporaryBlobContainer _blobStorageContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientFactoryBuilderExtensionsTests" /> class.
        /// </summary>
        public AzureClientFactoryBuilderExtensionsTests(EventHubsEntityFixture fixture, ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _logger = new XunitTestLogger(outputWriter);
            _eventHubsConfig = _config.GetEventHubs();

            EventHubsName = fixture.HubName;
        }

        private string EventHubsName { get; }

        [Fact]
        public async Task AddEventHubProducerClientWithNamespace_SendEvent_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionStringSecretName = "MyConnectionString";
            EventHubsConfig eventHubsConfig = _config.GetEventHubs();
            services.AddSecretStore(stores => stores.AddInMemory(connectionStringSecretName, eventHubsConfig.EventHubsConnectionString));

            // Act
            services.AddAzureClients(clients => clients.AddEventHubProducerClient(connectionStringSecretName, EventHubsName));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventHubProducerClient>>();
            await using (EventHubProducerClient client = factory.CreateClient("Default"))
            {
                SensorReading reading = SensorReadingGenerator.Generate();
                var eventData = new EventData(BinaryData.FromObjectAsJson(reading));

                await client.SendAsync(new[] { eventData });
                await RetryAssertUntilServiceBusMessageIsAvailableAsync(received =>
                {
                    var actual = received.EventBody.ToObjectFromJson<SensorReading>();
                    Assert.Equal(reading.SensorId, actual.SensorId);
                });
            }
        }

        private async Task RetryAssertUntilServiceBusMessageIsAvailableAsync(Action<EventData> assertion)
        {
            PolicyWrap policy = CreateRetryPolicy();
            EventProcessorClient eventProcessor = CreateEventProcessorClient();

            var isProcessed = false;
            var exceptions = new Collection<Exception>();
            eventProcessor.ProcessErrorAsync += args =>
            {
                exceptions.Add(args.Exception);
                return Task.CompletedTask;
            };
            eventProcessor.ProcessEventAsync += async args =>
            {
                try
                {
                    assertion(args.Data);
                    isProcessed = true;
                    await args.UpdateCheckpointAsync();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            };
            await eventProcessor.StartProcessingAsync();

            try
            { 
                policy.Execute(() =>
                {
                    if (!isProcessed)
                    {
                        if (exceptions.Count == 1)
                        {
                            throw exceptions[0];
                        }
                
                        throw new AggregateException(exceptions);
                    }
                });
            }
            finally
            {
                await eventProcessor.StopProcessingAsync();
            }
        }

        private EventProcessorClient CreateEventProcessorClient()
        {
            EventProcessorClient eventProcessor = _eventHubsConfig.GetProcessorClient(EventHubsName, _blobStorageContainer.Client);
            return eventProcessor;
        }

        private static PolicyWrap CreateRetryPolicy()
        {
            PolicyWrap policy =
                Policy.Timeout(TimeSpan.FromSeconds(30))
                      .Wrap(Policy.Handle<Exception>()
                                  .WaitAndRetryForever(index => TimeSpan.FromMilliseconds(500)));
            return policy;
        }

        public async Task InitializeAsync()
        {
            _connection = TemporaryManagedIdentityConnection.Create(_config, _logger);
            _blobStorageContainer = await TemporaryBlobContainer.CreateIfNotExistsAsync(_eventHubsConfig.Storage.Name, $"test-{Guid.NewGuid()}", _logger);
        }

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
