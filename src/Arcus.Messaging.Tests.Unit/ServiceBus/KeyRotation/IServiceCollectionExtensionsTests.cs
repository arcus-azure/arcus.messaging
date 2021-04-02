using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation.Extensions;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.KeyRotation
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestart_WithoutJobId_Fails(string jobId)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithAutoRestartServiceBusMessagePumpOnRotatedCredentials(
                    jobId,
                    "subscription-prefix",
                    "service bus topic connection string secret key",
                    "message pump connection string key"));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void WithAutoRestart_WithoutSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithAutoRestartServiceBusMessagePumpOnRotatedCredentials(
                    "job ID",
                    subscriptionPrefix,
                    "service bus topic connection string secret key",
                    "message pump connection string key"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void WithAutoRestart_WithoutSubscriptionTopicConnectionStringSecretKey_Fails(
            string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithAutoRestartServiceBusMessagePumpOnRotatedCredentials(
                    "job ID",
                    "subscription-prefix",
                    serviceBusTopicConnectionStringSecretKey,
                    "message pump connection string key"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void WithAutoRestart_WithoutMessagePumpConnectionStringKey_Fails(string messagePumpConnectionStringKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithAutoRestartServiceBusMessagePumpOnRotatedCredentials(
                    "job ID",
                    "subscription-prefix",
                    "service bus topic connection string secret key",
                    messagePumpConnectionStringKey));
        }

        [Fact]
        public void WithAutoRestart_WithoutRelatedMessagePump_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationRoot(new List<IConfigurationProvider>()));
            services.AddLogging();
            
            // Act
            services.WithAutoRestartServiceBusMessagePumpOnRotatedCredentials(
                "job ID",
                "subscription-prefix",
                "service bus topic connection string secret key",
                "message pump connection string key");
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var exception = Assert.ThrowsAny<InvalidOperationException>(() =>
                provider.GetRequiredService<IMessageHandler<CloudEvent, AzureServiceBusMessageContext>>());
            Assert.Contains("Cannot register re-authentication", exception.Message);
        }
    }
}
