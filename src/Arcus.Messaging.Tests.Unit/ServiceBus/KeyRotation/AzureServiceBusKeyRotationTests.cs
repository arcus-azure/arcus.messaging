using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Bogus;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest.Azure;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.KeyRotation
{
    [Trait(name: "Category", value: "Unit")]
    public class AzureServiceBusKeyRotationTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void CreateRotation_WithoutClient_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusKeyRotation(
                    serviceBusClient: null, 
                    authentication: Mock.Of<IKeyVaultAuthentication>(), 
                    configuration: Mock.Of<IKeyVaultConfiguration>(), 
                    logger: NullLogger.Instance));
        }

        [Fact]
        public void CreateRotation_WithoutAuthentication_Throws()
        {
            var location = new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntityType.Topic, "entity name", "authorization rule name");
            var client = new AzureServiceBusClient(Mock.Of<IAzureServiceBusManagementAuthentication>(), location, NullLogger.Instance);
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusKeyRotation(
                    serviceBusClient: client, 
                    authentication: null, 
                    configuration: Mock.Of<IKeyVaultConfiguration>(),
                    logger: NullLogger.Instance));
        }

        [Fact]
        public void CreateRotation_WithoutConfiguration_Throws()
        {
            var location = new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntityType.Topic, "entity name", "authorization rule name");
            var client = new AzureServiceBusClient(Mock.Of<IAzureServiceBusManagementAuthentication>(), location, NullLogger.Instance);
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusKeyRotation(
                    serviceBusClient: client,
                    authentication: Mock.Of<IKeyVaultAuthentication>(),
                    configuration: null,
                    logger: NullLogger.Instance));
        }

        [Fact]
        public void CreateRotation_WithoutLogger_Throws()
        {
            var location = new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntityType.Topic, "entity name", "authorization rule name");
            var client = new AzureServiceBusClient(Mock.Of<IAzureServiceBusManagementAuthentication>(), location, NullLogger.Instance);
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusKeyRotation(
                    serviceBusClient: client,
                    authentication: Mock.Of<IKeyVaultAuthentication>(),
                    configuration: Mock.Of<IKeyVaultConfiguration>(),
                    logger: null));
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Topic)]
        [InlineData(ServiceBusEntityType.Queue)]
        public async Task RotateServiceBusSecret_WithValidArguments_RotatesPrimarySecondaryAlternately(ServiceBusEntityType entity)
        {
            // Arrange
            string vaultUrl = BogusGenerator.Internet.UrlWithPath(protocol: "https");
            string secretName = BogusGenerator.Random.Word();
            AzureServiceBusNamespace @namespace = GenerateAzureServiceBusLocation(entity);
            var response = new AzureOperationResponse<AccessKeys>
            {
                Body = new AccessKeys(
                    primaryConnectionString: BogusGenerator.Random.Words(),
                    secondaryConnectionString: BogusGenerator.Random.Words())
            };

            Mock<ITopicsOperations> stubTopics = CreateStubTopicsOperations(@namespace, response);
            Mock<IQueuesOperations> stubQueues = CreateStubQueueOperations(@namespace, response);
            Mock<IAzureServiceBusManagementAuthentication> stubServiceBusAuthentication = CreateStubAuthentication(stubTopics.Object, stubQueues.Object);
            Mock<IKeyVaultAuthentication> stubKeyVaultAuthentication = CreateStubKeyVaultAuthentication(vaultUrl, secretName, response.Body);
            
            var rotation = new AzureServiceBusKeyRotation(
                new AzureServiceBusClient(stubServiceBusAuthentication.Object, @namespace, NullLogger.Instance), 
                stubKeyVaultAuthentication.Object, new KeyVaultConfiguration(vaultUrl), NullLogger.Instance);

            // Act
            await rotation.RotateServiceBusSecretAsync(secretName);

            // Assert
            Assert.Empty(DetermineNonRelevantInvocations(entity, stubTopics.Invocations, stubQueues.Invocations));
            Assert.Collection(DetermineRelevantInvocations(entity, stubTopics.Invocations, stubQueues.Invocations),
               invocation => AssertInvocationKeyRotation(invocation, KeyType.SecondaryKey),
               invocation => AssertInvocationKeyRotation(invocation, KeyType.PrimaryKey));

            await rotation.RotateServiceBusSecretAsync(secretName);
            Assert.Empty(DetermineNonRelevantInvocations(entity, stubTopics.Invocations, stubQueues.Invocations));
            Assert.Collection(DetermineRelevantInvocations(entity, stubTopics.Invocations, stubQueues.Invocations).Skip(2),
               invocation => AssertInvocationKeyRotation(invocation, KeyType.PrimaryKey),
               invocation => AssertInvocationKeyRotation(invocation, KeyType.SecondaryKey));
        }

        private static AzureServiceBusNamespace GenerateAzureServiceBusLocation(ServiceBusEntityType entity)
        {
            var location = new AzureServiceBusNamespace(
                resourceGroup: BogusGenerator.Random.Word(),
                @namespace: BogusGenerator.Random.Word(),
                entity: entity,
                entityName: BogusGenerator.Random.Word(),
                authorizationRuleName: BogusGenerator.Random.Word());

            return location;
        }

        private static Mock<IAzureServiceBusManagementAuthentication> CreateStubAuthentication(ITopicsOperations topicsOperations, IQueuesOperations queuesOperations)
        {
            var stubManagementClient = new Mock<IServiceBusManagementClient>();
            stubManagementClient.Setup(c => c.Topics).Returns(topicsOperations);
            stubManagementClient.Setup(c => c.Queues).Returns(queuesOperations);

            var stubAuthentication = new Mock<IAzureServiceBusManagementAuthentication>();
            stubAuthentication.Setup(a => a.AuthenticateAsync())
                              .ReturnsAsync(stubManagementClient.Object);

            return stubAuthentication;
        }

        private static Mock<IQueuesOperations> CreateStubQueueOperations(
            AzureServiceBusNamespace @namespace, 
            AzureOperationResponse<AccessKeys> response)
        {
            var stubQueue = new Mock<IQueuesOperations>();
            stubQueue.Setup(q => q.RegenerateKeysWithHttpMessagesAsync(
                @namespace.ResourceGroup, @namespace.Namespace, @namespace.EntityName, @namespace.AuthorizationRuleName, It.IsAny<RegenerateAccessKeyParameters>(), null, default))
                     .ReturnsAsync(response);

            return stubQueue;
        }

        private static Mock<ITopicsOperations> CreateStubTopicsOperations(
            AzureServiceBusNamespace @namespace,
            AzureOperationResponse<AccessKeys> response)
        {
            var stubTopics = new Mock<ITopicsOperations>();
            stubTopics.Setup(t => t.RegenerateKeysWithHttpMessagesAsync(
                @namespace.ResourceGroup, @namespace.Namespace, @namespace.EntityName, @namespace.AuthorizationRuleName, It.IsAny<RegenerateAccessKeyParameters>(), null, default))
                      .ReturnsAsync(response);

            return stubTopics;
        }

        private static Mock<IKeyVaultAuthentication> CreateStubKeyVaultAuthentication(string vaultUrl, string secretName, AccessKeys keys)
        {
            var stubKeyVaultClient = new Mock<IKeyVaultClient>();
            stubKeyVaultClient.Setup(c => c.SetSecretWithHttpMessagesAsync(
                vaultUrl, secretName, It.Is<string>(value => value == keys.PrimaryConnectionString || value == keys.SecondaryConnectionString), null, null, null, null, default))
                              .ReturnsAsync(new AzureOperationResponse<SecretBundle>());

            var stubKeyVaultAuthentication = new Mock<IKeyVaultAuthentication>();
            stubKeyVaultAuthentication.Setup(a => a.AuthenticateAsync())
                                      .ReturnsAsync(stubKeyVaultClient.Object);
           
            return stubKeyVaultAuthentication;
        }

        private static IEnumerable<IInvocation> DetermineRelevantInvocations(
            ServiceBusEntityType entity,
            IEnumerable<IInvocation> topicInvocations,
            IEnumerable<IInvocation> queueInvocations)
        {
            return entity switch
            {
                ServiceBusEntityType.Topic => topicInvocations,
                ServiceBusEntityType.Queue => queueInvocations,
                ServiceBusEntityType.Unknown => throw new ArgumentOutOfRangeException(nameof(entity), "Don't support 'Unknown' Azure Service Bus entity type here"),
                _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Azure Service Bus entity type should either be 'Topic' or 'Queue'")
            };
        }

        private static IEnumerable<IInvocation> DetermineNonRelevantInvocations(
            ServiceBusEntityType entity,
            IEnumerable<IInvocation> topicInvocations,
            IEnumerable<IInvocation> queueInvocations)
        {
            return entity == ServiceBusEntityType.Topic ? queueInvocations : topicInvocations;
        }

        private static void AssertInvocationKeyRotation(IInvocation invocation, KeyType keyType)
        {
            object item = Assert.Single(invocation.Arguments, arg => arg is RegenerateAccessKeyParameters);
            Assert.NotNull(item);
            var parameters = Assert.IsType<RegenerateAccessKeyParameters>(item);
            Assert.Equal(keyType, parameters.KeyType);
        }
    }
}
