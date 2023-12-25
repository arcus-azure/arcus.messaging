using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Wrap;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.ServiceBus
{
    public class AzureClientFactoryBuilderExtensionsTests
    {
        private readonly TestConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientFactoryBuilderExtensionsTests" /> class.
        /// </summary>
        public AzureClientFactoryBuilderExtensionsTests()
        {
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task AddServiceBusClient_SendsMessage_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionStringSecretName = "MyConnectionString";
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);
            services.AddSecretStore(stores => stores.AddInMemory(connectionStringSecretName, connectionString));
            
            // Act
            services.AddAzureClients(clients => clients.AddServiceBusClient(connectionStringSecretName));
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
            await using (ServiceBusClient client = factory.CreateClient("Default"))
            await using (ServiceBusSender sender = client.CreateSender(connectionStringProperties.EntityPath))
            {
                var order = OrderGenerator.Generate();
                var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(order));
                await sender.SendMessageAsync(message);

                await RetryAssertUntilServiceBusMessageIsAvailableAsync(client, connectionStringProperties.EntityPath, msg =>
                {
                    var actual = msg.Body.ToObjectFromJson<Order>();
                    Assert.Equal(order.Id, actual.Id);
                });
            }
        }

        private static async Task RetryAssertUntilServiceBusMessageIsAvailableAsync(ServiceBusClient client, string entityPath, Action<ServiceBusReceivedMessage> assertion)
        {
            AsyncPolicyWrap policy =
                Policy.TimeoutAsync(TimeSpan.FromSeconds(30))
                      .WrapAsync(Policy.Handle<Exception>()
                                       .WaitAndRetryForeverAsync(index => TimeSpan.FromMilliseconds(500)));

            await using (ServiceBusReceiver receiver = client.CreateReceiver(entityPath))
            {
                await policy.ExecuteAsync(async () =>
                {
                    IAsyncEnumerable<ServiceBusReceivedMessage> messages = receiver.ReceiveMessagesAsync();
                    var exceptions = new Collection<Exception>();

                    await foreach (ServiceBusReceivedMessage message in messages)
                    {
                        try
                        {
                            assertion(message);
                            await receiver.CompleteMessageAsync(message);
                            
                            return;
                        }
                        catch (Exception exception)
                        {
                            exceptions.Add(exception);
                        }
                    }

                    throw exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions);
                });
            }
        }
    }
}
