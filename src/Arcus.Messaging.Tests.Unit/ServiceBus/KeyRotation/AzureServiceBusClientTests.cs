using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Bogus;
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
    public class AzureServiceBusClientTests
    {
        [Fact]
        public void CreateClient_WithoutAuthentication_Throws()
        {
            var location = new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntityType.Topic, "entity name", "authorization rule name");
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusClient(authentication: null, @namespace: location, logger: NullLogger.Instance));
        }

        [Fact]
        public void CreateClient_WithoutLocation_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusClient(Mock.Of<IAzureServiceBusManagementAuthentication>(), @namespace: null, logger: NullLogger.Instance));
        }

        [Fact]
        public void CreateClient_WithoutLogger_Throws()
        {
            var location = new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntityType.Topic, "entity name", "authorization rule name");
            Assert.ThrowsAny<ArgumentException>(
                () => new AzureServiceBusClient(Mock.Of<IAzureServiceBusManagementAuthentication>(), location, logger: null));
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Topic, KeyType.PrimaryKey)]
        [InlineData(ServiceBusEntityType.Topic, KeyType.SecondaryKey)]
        [InlineData(ServiceBusEntityType.Queue, KeyType.PrimaryKey)]
        [InlineData(ServiceBusEntityType.Queue, KeyType.SecondaryKey)]
        public async Task RotateConnectionStringKey_WithSpecifiedEntity_RotatesKeyForEntity(ServiceBusEntityType entity, KeyType keyType)
        {
            // Arrange
            var generator = new Faker();
            string resourceGroup = generator.Random.Word();
            string @namespace = generator.Random.Word();
            string entityName = generator.Random.Word();
            string authorizationRuleName = generator.Random.Word();

            string primaryConnectionString = generator.Random.Words();
            string secondaryConnectionString = generator.Random.Words();
            var response = new AzureOperationResponse<AccessKeys>
            {
                Body = new AccessKeys(
                    primaryConnectionString: primaryConnectionString, 
                    secondaryConnectionString: secondaryConnectionString)
            };

            Mock<ITopicsOperations> stubTopics = CreateStubTopicsOperations(resourceGroup, @namespace, entityName, authorizationRuleName, response);
            Mock<IQueuesOperations> stubQueues = CreateStubQueueOperations(resourceGroup, @namespace, entityName, authorizationRuleName, response);
            Mock<IAzureServiceBusManagementAuthentication> stubAuthentication = CreateStubAuthentication(stubTopics.Object, stubQueues.Object);

            var client = new AzureServiceBusClient(
                stubAuthentication.Object, 
                new AzureServiceBusNamespace(resourceGroup, @namespace, entity, entityName, authorizationRuleName),
                NullLogger.Instance);

            // Act
            string connectionString = await client.RotateConnectionStringKeyAsync(keyType);

            // Assert
            Assert.Equal(primaryConnectionString == connectionString, KeyType.PrimaryKey == keyType);
            Assert.Equal(secondaryConnectionString == connectionString, KeyType.SecondaryKey == keyType);
            Assert.Equal(stubTopics.Invocations.Count == 1 && stubQueues.Invocations.Count == 0, ServiceBusEntityType.Topic == entity);
            Assert.Equal(stubTopics.Invocations.Count == 0 && stubQueues.Invocations.Count == 1, ServiceBusEntityType.Queue == entity);
        }

        private static Mock<IAzureServiceBusManagementAuthentication> CreateStubAuthentication(ITopicsOperations topicsOperations, IQueuesOperations queuesOperations)
        {
            var stubManagementClient = new Mock<ServiceBusManagementClient>(new HttpClient(), true);
            stubManagementClient.Setup(c => c.Topics).Returns(topicsOperations);
            stubManagementClient.Setup(c => c.Queues).Returns(queuesOperations);

            var stubAuthentication = new Mock<IAzureServiceBusManagementAuthentication>();
            stubAuthentication.Setup(a => a.AuthenticateAsync())
                              .ReturnsAsync(stubManagementClient.Object);
            
            return stubAuthentication;
        }

        private static Mock<IQueuesOperations> CreateStubQueueOperations(
            string resourceGroup,
            string @namespace,
            string entityName,
            string authorizationRuleName,
            AzureOperationResponse<AccessKeys> response)
        {
            var stubQueue = new Mock<IQueuesOperations>();
            stubQueue.Setup(q => q.RegenerateKeysWithHttpMessagesAsync(
                                resourceGroup, @namespace, entityName, authorizationRuleName, It.IsAny<RegenerateAccessKeyParameters>(), null, default))
                     .ReturnsAsync(response);
            
            return stubQueue;
        }

        private static Mock<ITopicsOperations> CreateStubTopicsOperations(
            string resourceGroup,
            string @namespace,
            string entityName,
            string authorizationRuleName,
            AzureOperationResponse<AccessKeys> response)
        {
            var stubTopics = new Mock<ITopicsOperations>();
            stubTopics.Setup(t => t.RegenerateKeysWithHttpMessagesAsync(
                                 resourceGroup, @namespace, entityName, authorizationRuleName, It.IsAny<RegenerateAccessKeyParameters>(), null, default))
                      .ReturnsAsync(response);
            
            return stubTopics;
        }
    }
}
