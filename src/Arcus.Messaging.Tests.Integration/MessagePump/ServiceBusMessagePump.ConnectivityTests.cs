using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Security.Core.Caching.Configuration;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Microsoft.Extensions.Logging.ServiceBusEntityType;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusQueueMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessageForHierarchical_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, format: MessageCorrelationFormat.Hierarchical, configureOptions: options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt =>
                       {
                           opt.AutoComplete = true;
                           opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                       })
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var properties = ServiceBusConnectionStringProperties.Parse(QueueConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(properties.EntityPath, _ => namespaceConnectionString, opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Topic, options =>
            {
                options.AddServiceBusTopicMessagePump(TopicConnectionString)
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var properties = ServiceBusConnectionStringProperties.Parse(TopicConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(Topic, options =>
            {
                options.AddServiceBusTopicMessagePump(
                           topicName: properties.EntityPath,
                           subscriptionName: Guid.NewGuid().ToString(),
                           getConnectionStringFromConfigurationFunc: _ => namespaceConnectionString,
                           configureMessagePump: opt =>
                           {
                               opt.AutoComplete = true;
                               opt.TopicSubscription = TopicSubscription.Automatic;
                           })
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpUsingManagedIdentity_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(TopicConnectionString);
            using var auth = TemporaryManagedIdentityConnection.Create(_config, _logger);
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(Topic, options =>
            {
                options.AddServiceBusTopicMessagePumpUsingManagedIdentity(
                           topicName: properties.EntityPath,
                           subscriptionName: Guid.NewGuid().ToString(),
                           serviceBusNamespace: properties.FullyQualifiedNamespace,
                           clientId: auth.ClientId,
                           configureMessagePump: opt =>
                           {
                               opt.AutoComplete = true;
                               opt.TopicSubscription = TopicSubscription.Automatic;
                           })
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpUsingManagedIdentity_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(QueueConnectionString);
            using var auth = TemporaryManagedIdentityConnection.Create(_config, _logger);

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(
                           queueName: properties.EntityPath,
                           serviceBusNamespace: properties.FullyQualifiedNamespace,
                           clientId: auth.ClientId,
                           configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

         [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeys_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            string tenantId = _config.GetTenantId();
            KeyRotationConfig keyRotationConfig = _config.GetKeyRotationConfig();
            _logger.LogInformation("Using Service Principal [ClientID: '{ClientId}']", keyRotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusConfiguration(keyRotationConfig, _logger);
            string freshConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

            SecretClient secretClient = CreateSecretClient(tenantId, keyRotationConfig);
            await SetConnectionStringInKeyVaultAsync(secretClient, keyRotationConfig, freshConnectionString);

            var options = new WorkerOptions();
            options.AddSecretStore(stores => stores.AddAzureKeyVaultWithServicePrincipal(
                       rawVaultUri: keyRotationConfig.KeyVault.VaultUri,
                       tenantId: tenantId,
                       clientId: keyRotationConfig.ServicePrincipal.ClientId,
                       clientKey: keyRotationConfig.ServicePrincipal.ClientSecret,
                       cacheConfiguration: CacheConfiguration.Default))
                   .AddServiceBusQueueMessagePump(keyRotationConfig.KeyVault.SecretName, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(secretClient, keyRotationConfig, newSecondaryConnectionString);

                // Act
                string newPrimaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                // Assert
                var producer = new TestServiceBusMessageProducer(newPrimaryConnectionString);
                await producer.ProduceAsync(message);

                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData);
            }
        }

        private static SecretClient CreateSecretClient(string tenantId, KeyRotationConfig keyRotationConfig)
        {
            var clientCredential = new ClientSecretCredential(tenantId,
                keyRotationConfig.ServicePrincipal.ClientId,
                keyRotationConfig.ServicePrincipal.ClientSecret);
            
            var secretClient = new SecretClient(new Uri(keyRotationConfig.KeyVault.VaultUri), clientCredential);
            return secretClient;
        }

        private static async Task SetConnectionStringInKeyVaultAsync(SecretClient keyVaultClient, KeyRotationConfig keyRotationConfig, string rotatedConnectionString)
        {
            await keyVaultClient.SetSecretAsync(keyRotationConfig.KeyVault.SecretName, rotatedConnectionString);
        }
    }
}
