using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Security.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public async Task AddServiceBusTopicMessagePump_WithSubscriptionNameIndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result = 
                services.AddServiceBusTopicMessagePump(
                    "topic name", "subscription name", "secret name", options => options.AutoComplete = true);
            
            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.Services.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            await Assert.ThrowsAnyAsync<Exception>(() => messagePump.StartAsync(CancellationToken.None));
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }

        [Fact]
        public void AddServiceBusTopicMessagePump_WithCustomJobId_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();
            string jobId = Guid.NewGuid().ToString();

            // Act
            ServiceBusMessageHandlerCollection result = 
                services.AddServiceBusTopicMessagePump(
                    "topic name", "subscription name", "secret name", options => options.JobId = jobId);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);
            Assert.Equal(jobId, result.JobId);
        }

        [Fact]
        public async Task AddServiceBusTopicMessagePump_WithTopicNameAndSubscriptionNameIndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result = 
                services.AddServiceBusTopicMessagePump(
                    "topic name", "subscription name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.Services.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            await Assert.ThrowsAnyAsync<Exception>(() => messagePump.StartAsync(CancellationToken.None));
            spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
        }

        [Fact]
        public void AddServiceBusTopicMessagePump_WithSubscriptionNameConfig_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePump("subscription name", (IConfiguration config) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusTopicMessagePump_WithSubscriptionNameSecretProvider_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePump("subscription name", (ISecretProvider secretProvider) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusTopicMessagePump_WithSubscriptionNameSecretName_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePump("subscription name", "secret name");
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void AddServiceBusTopicMessagePump_WithTopicNameSubscriptionNameConfig_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePump("topic name", "subscription name", (IConfiguration config) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusTopicMessagePump_WithTopicNameSubscriptionNameSecretProvider_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePump("topic name", "subscription name", (ISecretProvider secretProvider) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusTopicMessagePump_WithTopicNameSubscriptionNameSecretName_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePump("topic name", "subscription name", "secret name");
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void AddServiceBusTopicMessagePumpWithPrefix_WithSubscriptionNameConfig_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix("subscription prefix", (IConfiguration config) => null);

            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void AddServiceBusTopicMessagePumpWithPrefix_WithSubscriptionNameSecretProvider_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix("subscription prefix", (ISecretProvider secretProvider) => null);

            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void AddServiceBusTopicMessagePumpWithPrefix_WithSubscriptionNameSecretName_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix("subscription prefix", "secret name");

            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void AddServiceBusTopicMessagePumpWithPrefix_WithTopicNameSubscriptionNameConfig_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix("topic name", "subscription prefix", (IConfiguration config) => null);

            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void AddServiceBusTopicMessagePumpWithPrefix_WithTopicNameSubscriptionNameSecretProvider_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix("topic name", "subscription prefix", (ISecretProvider secretProvider) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusTopicMessagePumpWithPrefix_WithTopicNameSubscriptionNameSecretName_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpWithPrefix("topic name", "subscription prefix", "secret name");
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusTopicMessagePumpUsingManagedIdentity_WithTopicNameSubscriptionNameSecretName_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpUsingManagedIdentity("topic name", "subscription name", "service bus namespace");
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusTopicMessagePumpUsingManagedIdentityWithPrefix_WithTopicNameSubscriptionNameSecretName_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusTopicMessagePumpUsingManagedIdentityWithPrefix("topic name", "subscription prefix", "service bus namespace");
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public async Task AddServiceBusQueueMessagePump_IndirectSecretProviderWithQueueName_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump(
                    "queue name", "secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.Services.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            try
            {
                await messagePump.StartAsync(CancellationToken.None);
            }
            finally
            {
                spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
            }
        }

        [Fact]
        public async Task AddServiceBusQueueMessagePump_IndirectSecretProviderWithoutQueueName_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var spySecretProvider = new Mock<ISecretProvider>();
            services.AddSingleton(serviceProvider => spySecretProvider.Object);
            services.AddSingleton(serviceProvider => Mock.Of<IConfiguration>());
            services.AddLogging();

            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump("secret name", configureMessagePump: options => options.AutoComplete = true);

            // Assert
            Assert.NotNull(result);
            ServiceProvider provider = result.Services.BuildServiceProvider();

            var messagePump = provider.GetService<IHostedService>();
            Assert.IsType<AzureServiceBusMessagePump>(messagePump);

            try
            {
                await messagePump.StartAsync(CancellationToken.None);
            }
            finally
            {
                spySecretProvider.Verify(spy => spy.GetRawSecretAsync("secret name"), Times.Once);
            }
        }
        
        [Fact]
        public void AddServiceBusQueueMessagePump_WithSecretProvider_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump((ISecretProvider secretProvider) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void AddServiceBusQueueMessagePump_WithCustomId_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            string jobId = Guid.NewGuid().ToString();

            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump("queue-name", "secret-name", options => options.JobId = jobId);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.IsType<AzureServiceBusMessagePump>(provider.GetRequiredService<IHostedService>());
            Assert.Equal(jobId, result.JobId);
        }
        
        [Fact]
        public void AddServiceBusQueueMessagePump_WithConfiguration_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump((IConfiguration config) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusQueueMessagePump_WithQueueNameSecretName_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump("queue name", "secret name");
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusQueueMessagePump_WithQueueNameSecretProvider_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump("queue name", (ISecretProvider secretProvider) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusQueueMessagePump_WithQueueNameConfiguration_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePump("queue name", (IConfiguration config) => null);
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }
        
        [Fact]
        public void AddServiceBusQueueMessagePumpUsingManagedIdentity_WithQueueNameConfiguration_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISecretProvider>());
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            
            // Act
            ServiceBusMessageHandlerCollection result =
                services.AddServiceBusQueueMessagePumpUsingManagedIdentity("queue name", "service bus namespace");
            
            // Assert
            Assert.NotNull(result);
            IServiceProvider provider = result.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageCorrelationInfoAccessor>());
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutContextFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(messageBodyFilter: null));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithBodyFilterWithoutContextFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutBodyFilterWithContextFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodyFilter: null,
                    messageContextFilter: context => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutImplementationFactory_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: null));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutImplementationFactoryWithContextFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: null,
                    messageContextFilter: context => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithImplementationFactoryWithoutContextFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler(), 
                    messageContextFilter: null));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutImplementationFactoryWithBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithImplementationFactoryWithoutBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler(), 
                    messageBodyFilter: null));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutImplementationFactoryWithContextFilterWithBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: null,
                    messageContextFilter: context => true,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithImplementationFactoryWithoutContextFilterWithBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: servivceProvider => new TestServiceBusMessageHandler(), 
                    messageContextFilter: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithImplementationFactoryWithContextFilterWithoutBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler(), 
                    messageContextFilter: context => true,
                    messageBodyFilter: null));
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandler_WithValidType_RegistersInterface()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act
            collection.WithServiceBusFallbackMessageHandler<PassThruServiceBusFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            var messageHandler = provider.GetRequiredService<IAzureServiceBusFallbackMessageHandler>();

            Assert.IsType<PassThruServiceBusFallbackMessageHandler>(messageHandler);
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandler_WithValidImplementationFunction_RegistersInterface()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var expected = new PassThruServiceBusFallbackMessageHandler();

            // Act
            collection.WithServiceBusFallbackMessageHandler(serviceProvider => expected);

            // Assert
            IServiceProvider provider = collection.Services.BuildServiceProvider();
            var actual = provider.GetRequiredService<IAzureServiceBusFallbackMessageHandler>();

            Assert.Same(expected, actual);
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandlerType_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((ServiceBusMessageHandlerCollection) null).WithServiceBusFallbackMessageHandler<PassThruServiceBusFallbackMessageHandler>());
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandlerImplementationFunction_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((ServiceBusMessageHandlerCollection) null).WithServiceBusFallbackMessageHandler(serviceProvider => new PassThruServiceBusFallbackMessageHandler()));
        }

        [Fact]
        public void WithServiceBusFallbackMessageHandlerImplementationFunction_WithoutImplementationFunction_Throws()
        {
            // Arrange
            var services = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusFallbackMessageHandler(createImplementation: (Func<IServiceProvider, PassThruServiceBusFallbackMessageHandler>)null));
        }
    }
}
