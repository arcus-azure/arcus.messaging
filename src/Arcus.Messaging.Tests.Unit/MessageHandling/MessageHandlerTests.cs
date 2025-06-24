using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Stubs;
using Arcus.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    [Trait("Category", "Unit")]
    public class MessageHandlerTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerTests"/> class.
        /// </summary>
        public MessageHandlerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public void Add_WithJobId_AdaptsMessageContextFilter()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection()) { JobId = jobId };
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>();
            IServiceProvider provider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(provider, _logger);

            // Assert
            Assert.NotNull(messageHandlers);
            MessageHandler handler = Assert.Single(messageHandlers);
            Assert.NotNull(handler);

            Assert.True(handler.CanProcessMessageBasedOnContext(AzureServiceBusMessageContextFactory.Generate(jobId)), "message handler should be able to process message based on context when using the same job ID");
            Assert.False(handler.CanProcessMessageBasedOnContext(AzureServiceBusMessageContextFactory.Generate("other-job-id")), "message handler should not be able to process message based on context when using a different job ID");
        }

        [Fact]
        public async Task CustomMessageHandlerConstructor_WithDefaultContext_SubtractsRegistration()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>();
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
        }

        [Fact]
        public async Task CustomMessageHandlerFactory_WithDefaultContext_SubtractsRegistration()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var spyHandler = new TestServiceBusMessageHandler();
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(_ => spyHandler);
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = AzureServiceBusMessageContextFactory.Generate(collection.JobId);
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
            Assert.True(spyHandler.IsProcessed);
        }

        [Fact]
        public async Task CustomMessageHandlerConstructor_WithCustomContext_SubtractsRegistration()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>();
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
        }

        [Fact]
        public async Task CustomMessageHandlerFactory_WithCustomContext_SubtractsRegistration()
        {
            // Arrange
            var spyHandler = new TestServiceBusMessageHandler();

            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(_ => spyHandler);
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = AzureServiceBusMessageContextFactory.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
            Assert.True(spyHandler.IsProcessed);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void CustomMessageHandlerFactory_WithMessageBodyAndContextFilter_SubtractsRegistration(bool matchesBody, bool matchesContext)
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            var spyHandler = new TestServiceBusMessageHandler();
            collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                _ => spyHandler,
                options => options.AddMessageContextFilter(_ => matchesContext)
                                  .AddMessageBodyFilter(_ => matchesBody));
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var context = AzureServiceBusMessageContextFactory.Generate(collection.JobId);
            Assert.Equal(matchesContext, messageHandler.CanProcessMessageBasedOnContext(messageContext: context));
            Assert.Equal(matchesBody, messageHandler.CanProcessMessageBasedOnMessage(new TestMessage()));
        }

        [Fact]
        public void SubtractsMessageHandlers_SelectsAllRegistrations()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<string>, string>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<Exception>, Exception>();
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>(_ => new StubServiceBusMessageHandler<TestMessage>());
            collection.WithServiceBusMessageHandler<StubServiceBusMessageHandler<TestMessage>, TestMessage>();

            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            Assert.Equal(4, messageHandlers.Count());
        }
    }
}
