﻿using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Microsoft.Extensions.Logging.ServiceBusEntityType;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
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
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(QueueName, _ => NamespaceConnectionString, opt => opt.AutoComplete = true)
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
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeys_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            await using TemporaryServiceBusNamespace serviceBus = await CreateServiceBusNamespaceAsync();
            await using TemporaryQueue queue = await CreateServiceBusQueueAsync(serviceBus.Config);

            ServiceBusAccessKeys keys = await serviceBus.GetAccessKeysAsync();
            await using TemporaryKeyVaultSecret secret = await CreateKeyVaultSecretAsync(keys.PrimaryConnectionString);

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddSecretStore(stores => AddAzureKeyVaultWithServicePrincipal(stores, _serviceBusConfig.ServicePrincipal))
                   .AddServiceBusQueueMessagePump(queue.Name, secret.Name)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            await using var worker = await Worker.StartNewAsync(options);

            // Act
            ServiceBusAccessKeys newKeys = await serviceBus.RotateAccessKeysAsync(ServiceBusAccessKeyType.PrimaryKey);

            // Assert
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();
            var producer = new TestServiceBusMessageProducer(queue.Name, serviceBus.Config);
            await producer.ProduceAsync(message);

            await secret.UpdateSecretAsync(newKeys.PrimaryConnectionString);

            OrderCreatedEventData actual = await ConsumeOrderCreatedAsync(message.MessageId, TimeSpan.FromSeconds(20));
            AssertReceivedOrderEventDataForW3C(message, actual);
        }

        private async Task<TemporaryServiceBusNamespace> CreateServiceBusNamespaceAsync()
        {
            return await TemporaryServiceBusNamespace.CreateBasicAsync(
                _config.GetSubscriptionId(),
                _config.GetResourceGroupName(),
                _config.GetServicePrincipal(),
                _logger);
        }

        private async Task<TemporaryQueue> CreateServiceBusQueueAsync(ServiceBusConfig serviceBus)
        {
            return await TemporaryQueue.CreateIfNotExistsAsync(serviceBus.HostName, $"queue-{Guid.NewGuid()}", _logger);
        }

        private async Task<TemporaryKeyVaultSecret> CreateKeyVaultSecretAsync(string secretValue)
        {
            return await TemporaryKeyVaultSecret.CreateAsync(
                $"Queue-ConnectionString-{Guid.NewGuid()}",
                secretValue,
                _config.GetKeyVault(),
                _logger);
        }

        private void AddAzureKeyVaultWithServicePrincipal(SecretStoreBuilder stores, ServicePrincipal servicePrincipal)
        {
            stores.AddAzureKeyVaultWithServicePrincipal(
                _config.GetKeyVault().VaultUri,
                servicePrincipal.TenantId,
                servicePrincipal.ClientId,
                servicePrincipal.ClientSecret);
        }
    }
}
