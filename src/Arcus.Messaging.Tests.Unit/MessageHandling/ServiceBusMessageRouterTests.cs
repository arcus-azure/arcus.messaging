using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Testing;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static Arcus.Messaging.Abstractions.MessageHandling.MessageProcessingError;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class ServiceBusMessageRouterTests
    {
        private readonly ILogger _logger;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageRouterTests"/> class.
        /// </summary>
        public ServiceBusMessageRouterTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task Route_WithoutRegisteredHandlers_FailsWithNoHandlers()
        {
            // Arrange
            var router = CreateMessageRouter();

            // Act
            MessageProcessingResult result = await router.RouteAnyMessageAsync();

            // Assert
            AssertResult.RouteFailed(CannotFindMatchedHandler, result, "no", "registered", "handlers");
        }

        [Fact]
        public async Task Route_WithoutMatchingRegisteredHandler_FailsWithNoMatchedHandler()
        {
            // Arrange
            var message = Messages.Any;
            var context = Contexts.Any;

            var router = new TestMessageRouterBuilder(_logger)
                .Build();

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(message, context);

            // Assert
            AssertResult.RouteFailed(CannotFindMatchedHandler, result, "no", "matched", "handler");
        }

        private TestMessageRouter CreateMessageRouter(
            Action<MessageHandlerCollection> configureServices = null,
            Action<MessageRouterOptions> configureOptions = null)
        {
            return TestMessageRouter.CreateFor(_logger, configureServices, configureOptions);
        }

        private sealed class TestMessageRouterBuilder : MessageHandlerCollection
        {
            private readonly ILogger _logger;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestMessageRouterBuilder"/> class.
            /// </summary>
            public TestMessageRouterBuilder(ILogger logger) : base(new ServiceCollection())
            {
                _logger = logger;
            }

            public TestMessageRouterBuilder WithMessageHandler<TMessage, TMessageContext, TMessageHandler>(
                Func<IServiceProvider, TMessageHandler> implementationFactory,
                Action<MessageHandlerOptions<TMessage, TMessageContext>> configureOptions = null)
                where TMessageContext : MessageContext
                where TMessageHandler : IMessageHandler<TMessage, TMessageContext>
            {
                var options = new MessageHandlerOptions<TMessage, TMessageContext>();
                configureOptions?.Invoke(options);

                _logger.LogDebug("[Test:Setup] register '{MessageHandlerType}<{MessageType}, {MessageContextType}>' message handler", typeof(TMessageHandler).Name, typeof(TMessage).Name, typeof(TMessageContext).Name);

                Services.AddTransient(
                    provider => MessageHandler.Create(
                        implementationFactory(provider),
                        provider.GetRequiredService<ILogger<IMessageHandler<TMessage, TMessageContext>>>(),
                        JobId,
                        options.MessageBodyFilter,
                        options.MessageContextFilter,
                        options.MessageBodySerializer));

                return this;
            }

            public TestMessageRouter Build(Action<MessageRouterOptions> configureOptions = null)
            {
                return TestMessageRouter.CreateFor(this, _logger, configureOptions);
            }
        }

        private sealed class TestMessageRouter(TestMessageRouterBuilder collection, MessageRouterOptions options, ILogger logger)
            : MessageRouter(collection.Services.BuildServiceProvider(), options, logger)
        {
            private MessageHandlerTriggerHistory TriggerHistory => ServiceProvider.GetRequiredService<MessageHandlerTriggerHistory>();

            public static TestMessageRouter CreateFor(
                TestMessageRouterBuilder collection,
                ILogger logger,
                Action<MessageRouterOptions> configureOptions = null)
            {
                var options = new MessageRouterOptions();
                configureOptions?.Invoke(options);

                return new TestMessageRouter(collection, options, logger);
            }

            public Task<MessageProcessingResult> RouteAnyMessageAsync()
            {
                return RouteMessageAsync(Messages.Any, TestMessageContext.Generate());
            }

            public Task<MessageProcessingResult> RouteMessageAsync<TMessageContext>(ITestMessage message, TMessageContext messageContext)
                where TMessageContext : MessageContext
            {
                var messageBody = BinaryData.FromObjectAsJson(message);
                var messageCorrelation = new MessageCorrelationInfo("operation-id", "transaction-id");
                return RouteMessageThroughRegisteredHandlersAsync(ServiceProvider, messageBody.IsEmpty ? string.Empty : messageBody.ToString(), messageContext, messageCorrelation, CancellationToken.None);
            }

            public void ShouldBeRoutedThroughNone() => TriggerHistory.ShouldBeEmpty();

            public void ShouldBeRoutedThrough<TMessageHandler>() => TriggerHistory.ShouldBeFired(typeof(TMessageHandler));

            public void ShouldNotRouteThrough<TMessageHandler>() => TriggerHistory.ShouldNotBeFired(typeof(TMessageHandler));
        }
    }
}
