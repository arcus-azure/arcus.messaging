using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
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
            var router = CreateMessageRouter(services =>
            {
                services.WithMessageHandler()
            })

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(message, context);

            // Assert
            AssertResult.RouteFailed(CannotFindMatchedHandler, result, "no", "matched", "handler");
        }

        private SpyTestServiceBusMessageRouter CreateMessageRouter(
            Action<ServiceBusMessageHandlerCollection> configureServices = null,
            Action<MessageRouterOptions> configureOptions = null)
        {
            return SpyTestServiceBusMessageRouter.CreateFor(_logger, configureServices, configureOptions);
        }

        private sealed class SpyTestServiceBusMessageRouter(IServiceProvider serviceProvider, MessageRouterOptions options, ILogger logger)
            : MessageRouter(serviceProvider, options, logger)
        {
            private MessageHandlerTriggerHistory TriggerHistory => ServiceProvider.GetRequiredService<MessageHandlerTriggerHistory>();

            public static SpyTestServiceBusMessageRouter CreateFor(
                ILogger logger,
                Action<ServiceBusMessageHandlerCollection> configureServices = null,
                Action<MessageRouterOptions> configureOptions = null)
            {
                var services = new ServiceCollection();
                services.AddSingleton<MessageHandlerTriggerHistory>();

                configureServices?.Invoke(new ServiceBusMessageHandlerCollection(services));

                var options = new MessageRouterOptions();
                configureOptions?.Invoke(options);

                return new SpyTestServiceBusMessageRouter(services.BuildServiceProvider(), options, logger);
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
