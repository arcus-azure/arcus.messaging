using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Testing;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
#pragma warning disable 618

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    [Trait("Category", "Unit")]
    public class MessageHandlerTests
    {
        private readonly Faker _bogusGenerator = new Faker();
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
            var collection = new MessageHandlerCollection(new ServiceCollection()) { JobId = jobId };
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>();
            IServiceProvider provider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(provider, _logger);

            // Assert
            Assert.NotNull(messageHandlers);
            MessageHandler handler = Assert.Single(messageHandlers);
            Assert.NotNull(handler);

            Assert.True(handler.CanProcessMessageBasedOnContext(new MessageContext("message-id", jobId, new Dictionary<string, object>())));
            Assert.False(handler.CanProcessMessageBasedOnContext(new MessageContext("message-id", "other-job-id", new Dictionary<string, object>())));
        }

        [Fact]
        public async Task CustomMessageHandlerConstructor_WithDefaultContext_SubtractsRegistration()
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>();
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = TestMessageContext.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
        }

        [Fact]
        public async Task CustomMessageHandlerFactory_WithDefaultContext_SubtractsRegistration()
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            var spyHandler = new DefaultTestMessageHandler();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(provider => spyHandler);
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = new MessageContext("message-id", new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
            Assert.True(spyHandler.IsProcessed);
        }

        [Fact]
        public async Task CustomMessageHandlerConstructor_WithCustomContext_SubtractsRegistration()
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>();
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = TestMessageContext.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CustomMessageHandlerConstructor_WithMessageBodyFilter_SubtractsRegistration(bool matches)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>((TestMessage message) => matches);
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);
            Assert.Equal(matches, messageHandler.CanProcessMessageBasedOnMessage(new TestMessage()));
        }

        [Fact]
        public async Task CustomMessageHandlerFactory_WithCustomContext_SubtractsRegistration()
        {
            // Arrange
            var spyHandler = new TestMessageHandler();

            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(provider => spyHandler);
            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var message = new TestMessage();
            var context = TestMessageContext.Generate();
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await messageHandler.ProcessMessageAsync(message, context, correlationInfo, CancellationToken.None);
            Assert.True(spyHandler.IsProcessed);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CustomMessageHandlerFactory_WithContextFilter_SubtractsRegistration(bool matchesContext)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            var spyHandler = new TestMessageHandler();
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                (TestMessageContext messageContext) => matchesContext,
                provider => spyHandler);
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var context = TestMessageContext.Generate();
            Assert.Equal(matchesContext, messageHandler.CanProcessMessageBasedOnContext(messageContext: context));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void CustomMessageHandlerFactory_WithMessageBodyAndContextFilter_SubtractsRegistration(bool matchesBody, bool matchesContext)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            var spyHandler = new TestMessageHandler();
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                (TestMessageContext messageContext) => matchesContext,
                (TestMessage messageBody) => matchesBody,
                provider => spyHandler);
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var context = TestMessageContext.Generate();
            Assert.Equal(matchesContext, messageHandler.CanProcessMessageBasedOnContext(messageContext: context));
            Assert.Equal(matchesBody, messageHandler.CanProcessMessageBasedOnMessage(new TestMessage()));
        }

        [Fact]
        public void SubtractsMessageHandlers_SelectsAllRegistrations()
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<StubTestMessageHandler<string, MessageContext>, string>();
            collection.WithMessageHandler<StubTestMessageHandler<Exception, MessageContext>, Exception>();
            collection.WithMessageHandler<StubTestMessageHandler<TestMessage, MessageContext>, TestMessage>(provider => new StubTestMessageHandler<TestMessage, MessageContext>());
            collection.WithMessageHandler<StubTestMessageHandler<TestMessage, MessageContext>, TestMessage>();

            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            Assert.Equal(4, messageHandlers.Count());
        }

        [Fact]
        public void WithMultipleMessageHandlers_WithSameMessageType_RegistersBoth()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(message => message.TestProperty == "Some value");

            // Act
            services.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(message => message.TestProperty == "Some other value");

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.Collection(
                handlers,
                handler => Assert.True(handler.CanProcessMessageBasedOnMessage(new TestMessage { TestProperty = "Some value" })),
                handler => Assert.True(handler.CanProcessMessageBasedOnMessage(new TestMessage { TestProperty = "Some other value" })));
        }
    }
}
