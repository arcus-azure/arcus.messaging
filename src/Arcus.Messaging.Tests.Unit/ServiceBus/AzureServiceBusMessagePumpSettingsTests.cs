using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Security.Core;
using Arcus.Security.Core.Providers;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class AzureServiceBusMessagePumpSettingsTests
    {
        [Fact]
        public void CreateSettings_WithEntityScopedUsingConnectionString_Succeeds()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Queue;
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusQueueMessagePumpOptions());
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var namespaceConnectionString = 
                $"Endpoint=sb://arcus-messaging-integration-tests.servicebus.windows.net/;SharedAccessKeyName=MyAccessKeyName;SharedAccessKey={Guid.NewGuid()}";
            
            // Act / Assert
            var settings = new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                serviceBusEntity: entityType,
                getConnectionStringFromConfigurationFunc: null,
                getConnectionStringFromSecretFunc: secretProvider => Task.FromResult(namespaceConnectionString),
                options: options,
                serviceProvider: serviceProvider);
        }
        
        [Fact]
        public void CreateSettings_WithTokenCredential_Succeeds()
        {
            // Arrange
            var serviceBusNamespace = "arcus-messaging-integration-tests.servicebus.windows.net";
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var expected = $"entity-{Guid.NewGuid()}";
            var entityType = ServiceBusEntityType.Queue;
            var credential = new DefaultAzureCredential();
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusQueueMessagePumpOptions());
            
            // Act / Assert
            var settings = new AzureServiceBusMessagePumpSettings(
                entityName: expected,
                subscriptionName: null,
                serviceBusEntity: entityType,
                fullyQualifiedNamespace: serviceBusNamespace,
                tokenCredential: credential,
                options: options,
                serviceProvider: serviceProvider);
        }
        
        [Fact]
        public async Task GetEntityPath_WithNamespaceConnectionStringInsteadOfEntityScopedUsingConnectionString_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Queue;
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusQueueMessagePumpOptions());
            var secretProvider = new EnvironmentVariableSecretProvider();
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ISecretProvider>(secretProvider)
                .BuildServiceProvider();
            var namespaceConnectionString = 
                $"Endpoint=sb://arcus-messaging-integration-tests.servicebus.windows.net/;SharedAccessKeyName=MyAccessKeyName;SharedAccessKey={Guid.NewGuid()}";
            
            var settings = new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                serviceBusEntity: entityType,
                getConnectionStringFromConfigurationFunc: null,
                getConnectionStringFromSecretFunc: secretProvider => Task.FromResult(namespaceConnectionString),
                options: options,
                serviceProvider: serviceProvider);

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(() => settings.GetEntityPathAsync());
        }
        
        [Fact]
        public async Task GetEntityPath_WithNamespaceConnectionStringInsteadOfEntityScopedUsingTokenCredential_Fails()
        {
            // Arrange
            var serviceBusNamespace = "arcus-messaging-integration-tests.servicebus.windows.net";
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var expected = $"entity-{Guid.NewGuid()}";
            var entityType = ServiceBusEntityType.Queue;
            var credential = new DefaultAzureCredential();
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusQueueMessagePumpOptions());
            
            var settings = new AzureServiceBusMessagePumpSettings(
                entityName: expected,
                subscriptionName: null,
                serviceBusEntity: entityType,
                fullyQualifiedNamespace: serviceBusNamespace,
                tokenCredential: credential,
                options: options,
                serviceProvider: serviceProvider);
            
            // Act
            string actual = await settings.GetEntityPathAsync();
            
            // Assert
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void CreateSettings_WithNamespaceConnectionStringInsteadOfEntityScopedUsingTokenCredential_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Queue;
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var serviceBusNamespace = "arcus-messaging-integration-tests.servicebus.windows.net";
            var credential = new DefaultAzureCredential();
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusQueueMessagePumpOptions());
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                serviceBusEntity: entityType,
                fullyQualifiedNamespace: serviceBusNamespace,
                tokenCredential: credential,
                options: options,
                serviceProvider: serviceProvider));
        }

        [Fact]
        public void CreateSettings_WithoutEitherConnectionStringFunctions_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Topic;
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusTopicMessagePumpOptions());
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                serviceBusEntity: entityType,
                getConnectionStringFromConfigurationFunc: null,
                getConnectionStringFromSecretFunc: null,
                options: options,
                serviceProvider: serviceProvider));
        }

        [Fact]
        public void CreateOptions_WithUnknownServiceEntityTypeUsingTokenCredential_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Unknown;
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusTopicMessagePumpOptions());
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var credential = new DefaultAzureCredential();
            var serviceBusNamespace = "arcus-messaging-integration-tests.servicebus.windows.net";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                fullyQualifiedNamespace: serviceBusNamespace,
                serviceBusEntity: entityType,
                tokenCredential: credential,
                options: options,
                serviceProvider: serviceProvider));
        }
        
        [Fact]
        public void CreateOptions_WithUnknownServiceEntityTypeUsingConnectionString_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Unknown;
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusTopicMessagePumpOptions());
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                serviceBusEntity: entityType,
                getConnectionStringFromConfigurationFunc: config => "MyConnectionString",
                getConnectionStringFromSecretFunc: null,
                options: options,
                serviceProvider: serviceProvider));
        }
        
        [Fact]
        public void CreateOptions_WithoutOptionsUsingTokenCredential_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Queue;
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var credential = new DefaultAzureCredential();
            var serviceBusNamespace = "arcus-messaging-integration-tests.servicebus.windows.net";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                fullyQualifiedNamespace: serviceBusNamespace,
                serviceBusEntity: entityType,
                tokenCredential: credential,
                options: null,
                serviceProvider: serviceProvider));
        }
        
        [Fact]
        public void CreateOptions_WithoutOptionsUsingConnectionString_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Topic;
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                serviceBusEntity: entityType,
                getConnectionStringFromConfigurationFunc: config => "MyConnectionString",
                getConnectionStringFromSecretFunc: null,
                options: null,
                serviceProvider: serviceProvider));
        }
        
        [Fact]
        public void CreateOptions_WithoutServiceProviderUsingTokenCredential_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Queue;
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusTopicMessagePumpOptions());
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var credential = new DefaultAzureCredential();
            var serviceBusNamespace = "arcus-messaging-integration-tests.servicebus.windows.net";
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                fullyQualifiedNamespace: serviceBusNamespace,
                serviceBusEntity: entityType,
                tokenCredential: credential,
                options: options,
                serviceProvider: null));
        }
        
        [Fact]
        public void CreateOptions_WithoutServiceProviderUsingConnectionString_Fails()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Topic;
            var options = new AzureServiceBusMessagePumpConfiguration(new AzureServiceBusTopicMessagePumpOptions());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new AzureServiceBusMessagePumpSettings(
                entityName: null,
                subscriptionName: null,
                serviceBusEntity: entityType,
                getConnectionStringFromConfigurationFunc: config => "MyConnectionString",
                getConnectionStringFromSecretFunc: null,
                options: options,
                serviceProvider: null));
        }
    }
}
