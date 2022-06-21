using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Testing.Logging;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
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
        public void CustomMessageHandlerConstructor_WithDefaultContextFilter_SubtractsRegistration(bool matchesContext)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>((MessageContext messageContext) => matchesContext);
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var context = new MessageContext("message-id", new Dictionary<string, object>());
            Assert.Equal(matchesContext, messageHandler.CanProcessMessageBasedOnContext(context));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CustomMessageHandlerConstructor_WithDefaultMessageBodyFilter_SubtractsRegistration(bool matches)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>((TestMessage messageBody) => matches);
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);
            Assert.Equal(matches, messageHandler.CanProcessMessageBasedOnMessage(new TestMessage()));
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CustomMessageHandlerConstructor_WithContextFilter_SubtractsRegistration(bool matchesContext)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>((TestMessageContext messageContext) => matchesContext);
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
        [InlineData(false)]
        [InlineData(true)]
        public void CustomMessageHandlerConstructor_WithContextFilterObsolete_SubtractsRegistration(bool matchesContext)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>((TestMessageContext messageContext) => matchesContext);
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
        [InlineData(false)]
        [InlineData(true)]
        public void CustomMessageHandlerFactory_WithDefaultContextFilter_SubtractsRegistration(bool matchesContext)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            var spyHandler = new DefaultTestMessageHandler();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                (MessageContext messageContext) => matchesContext,
                provider => spyHandler);
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var context = new MessageContext("message-id", new Dictionary<string, object>());
            Assert.Equal(matchesContext, messageHandler.CanProcessMessageBasedOnContext(messageContext: context));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CustomMessageHandlerFactory_WithDefaultContextFilterObsolete_SubtractsRegistration(bool matchesContext)
        {
            // Arrange
            var collection = new MessageHandlerCollection(new ServiceCollection());
            var spyHandler = new DefaultTestMessageHandler();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>(
                (MessageContext messageContext) => matchesContext,
                provider => spyHandler);
            ServiceProvider serviceProvider = collection.Services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider, _logger);

            // Assert
            MessageHandler messageHandler = Assert.Single(messageHandlers);
            Assert.NotNull(messageHandler);

            var context = new MessageContext("message-id", new Dictionary<string, object>());
            Assert.Equal(matchesContext, messageHandler.CanProcessMessageBasedOnContext(messageContext: context));
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
        public async Task CustomMessageHandler_WithContextFilter_UsesFilterDuringSelection()
        {
            // Arrange
            var messageId = Guid.NewGuid().ToString();
            var message = new TestMessage { TestProperty = Guid.NewGuid().ToString() };
            string messageJson = JsonConvert.SerializeObject(message);
            var context = new TestMessageContext(messageId, new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            var spyHandler1 = new TestMessageHandler();
            var spyHandler2 = new TestMessageHandler();

            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: ctx => ctx.MessageId == "some other ID",
                implementationFactory: provider => spyHandler2);
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>(
                messageContextFilter: ctx => ctx.MessageId == messageId,
                implementationFactory: provider => spyHandler1);
            collection.WithMessageHandler<TestMessageHandler, TestMessage, TestMessageContext>();
            collection.WithMessageHandler<DefaultTestMessageHandler, TestMessage>();

            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();
            var router = new TestMessageRouter(serviceProvider, _logger);

            // Act
            await router.RouteMessageAsync(messageJson, context, correlationInfo, CancellationToken.None);

            // Assert
            Assert.True(spyHandler1.IsProcessed);
            Assert.False(spyHandler2.IsProcessed);
        }

        [Fact]
        public async Task CustomMessageHandler_WithContextFilter_UsesMessageTypeDuringSelection()
        {
            // Arrange
            var spyHandler = new StubTestMessageHandler<Purchase, MessageContext>();

            var collection = new MessageHandlerCollection(new ServiceCollection());
            collection.WithMessageHandler<StubTestMessageHandler<Order, MessageContext>, Order>();
            collection.WithMessageHandler<StubTestMessageHandler<Purchase, TestMessageContext>, Purchase, TestMessageContext>();
            collection.WithMessageHandler<StubTestMessageHandler<Purchase, MessageContext>, Purchase>(provider => spyHandler);

            IServiceProvider serviceProvider = collection.Services.BuildServiceProvider();
            var router = new TestMessageRouter(serviceProvider, _logger);

            var purchase = new Purchase
            {
                CustomerName = _bogusGenerator.Name.FullName(), 
                Price = _bogusGenerator.Commerce.Price()
            };
            string purchaseJson = JsonConvert.SerializeObject(purchase);
            var context = new MessageContext("message-id", new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            // Act
            await router.RouteMessageAsync(purchaseJson, context, correlationInfo, CancellationToken.None);

            // Assert
            Assert.True(spyHandler.IsProcessed);
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
