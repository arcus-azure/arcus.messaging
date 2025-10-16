using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Bogus;
using FsCheck;
using FsCheck.Fluent;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class MessageRouterTests
    {
        private readonly ITestOutputHelper _outputWriter;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouterTests"/> class.
        /// </summary>
        public MessageRouterTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task Route_WithoutRegisteredHandlers_FailsWithNoHandlers()
        {
            // Arrange
            var router = CreateMessageRouter([]);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.Any, Contexts.Any);

            // Assert
            router.ShouldNotFoundMatched(result, "no", "registered", "handlers").QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithoutMatchingRegisteredHandlerContextType_FailsWithNoMatchingHandlerExample()
        {
            // Arrange
            var router = CreateMessageRouter(
            [
                Handlers.Unrelated<NewStarDiscovered, DefaultMessageContext>(),
                Handlers.FailedT<FreshPizzaOrdered, DefaultMessageContext>(),
                Handlers.Unrelated<NewStarDiscovered, DefaultMessageContext>(),
            ]);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.NewStarDiscovered, Contexts.Transactional);

            // Assert
            router.ShouldNotFoundMatched(result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithoutMatchingRegisteredHandlerMessageType_FailsWithNoMatchingHandlerExample()
        {
            // Arrange
            var router = CreateMessageRouter(
            [
                Handlers.Unrelated<NewStarDiscovered, DefaultMessageContext>(),
                Handlers.FailedT<FreshPizzaOrdered, DefaultMessageContext>(),
            ]);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.HealthyTreePlanted, Contexts.Default);

            // Assert
            router.ShouldNotFoundMatched(result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithoutMatchingRegisteredHandlerViaFilters_FailsWithNoMatchingHandlerExample()
        {
            // Arrange
            var router = CreateMessageRouter(
            [
                Handlers.Success<NewStarDiscovered, DefaultMessageContext>(h => h.AddFailedMessageBodyFilter()),
                Handlers.Success<NewStarDiscovered, DefaultMessageContext>(h => h.AddFailedMessageContextFilter()),
            ]);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.NewStarDiscovered, Contexts.Default);

            // Assert
            router.ShouldNotFoundMatched(result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithMatchingHandlerViaCustomDeserialization_SucceedsByFiringHandlerExample()
        {
            // Arrange
            var matched = Handlers.Success<NewStarDiscovered, TransactionalMessageContext>(options =>
            {
                options.UseMessageBodyDeserializer(Deserializers.ForMessage<NewStarDiscovered>());
            });
            var router = CreateMessageRouter([matched]);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.FreshPizzaOrdered, Contexts.Transactional);

            // Assert
            router.ShouldRouteThrough(matched, result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithInvalidRegisteredHandlers_SucceedsBySkipingHandler()
        {
            // Arrange
            var router = CreateMessageRouter(
            [
                Handlers.Success<IComparable, DefaultMessageContext>(),
                Handlers.Success<FreshPizzaOrdered, DefaultMessageContext>(h => h.AddMessageBodyFilter(_ => throw new InvalidOperationException("sabotage body filter"))),
                Handlers.Success<FreshPizzaOrdered, DefaultMessageContext>(h => h.AddMessageContextFilter(_ => throw new InvalidOperationException("sabotage context filter")))
            ]);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.FreshPizzaOrdered, Contexts.Default);

            // Assert
            router.ShouldNotFoundMatched(result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithDifferentJobId_SucceedsBySkippingHandler()
        {
            // Arrange
            var handlerJobId = $"other-{Guid.NewGuid()}";
            var router = CreateMessageRouter(
            [
                Handlers.Success<NewStarDiscovered, TransactionalMessageContext>(h =>
                {
                    h.JobId = handlerJobId;
                })
            ]);

            var context = Contexts.Transactional;
            Assert.NotEqual(handlerJobId, context.JobId);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.NewStarDiscovered, context);

            // Assert
            router.ShouldNotFoundMatched(result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithIgnoreAdditionalMembersDeserialization_SucceedsByFiringHandler()
        {
            // Arrange
            var unexpectedMatched = Handlers.Success<HealthyTreePlanted, SpecializedMessageContext>();
            var router = CreateMessageRouter([unexpectedMatched], opt =>
            {
                opt.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore;
            });

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(Messages.NewStarDiscovered, Contexts.Specialized);

            // Assert
            router.ShouldRouteThrough(unexpectedMatched, result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public async Task Route_WithNullJsonMessageBody_SucceedsByIgnoring()
        {
            // Arrange
            var router = CreateMessageRouter([Handlers.Success<NewStarDiscovered, TransactionalMessageContext>()]);

            // Act
            MessageProcessingResult result = await router.RouteMessageAsync(null, Contexts.Transactional);

            // Assert
            router.ShouldNotFoundMatched(result).QuickCheckThrowOnFailure();
        }

        [Fact]
        public void Route_WithoutMatchingRegisteredHandler_FailsWithNoMatchedHandlerProperty()
        {
            Prop.ForAll(TestMessageHandlerRegistrations.ArbWithoutMatching, registrations =>
            {
                // Arrange
                var message = Messages.Any;
                var context = Contexts.Any;
                var registrationsFiltered = registrations.ExceptWith(message, context);

                return registrationsFiltered.Any().When(async () =>
                {
                    var router = CreateMessageRouter(Bogus.Random.Shuffle(registrationsFiltered));

                    // Act
                    MessageProcessingResult result = await router.RouteMessageAsync(message, context);

                    // Assert
                    return router.ShouldNotFoundMatched(result);
                });

            }).QuickCheckThrowOnFailure();
        }

        [Fact]
        public void Route_WithSingleMatchingRegisteredHandler_SucceedsByFiringHandlerProperty()
        {
            Prop.ForAll(TestMessageHandlerRegistrations.ArbWithoutMatching, async registrations =>
            {
                // Arrange
                var message = Messages.Any;
                var context = Contexts.Any;
                var matched = Handlers.Matched(message, context);
                var registrationsArr = registrations.ExceptWith(message, context);

                var router = CreateMessageRouter(Bogus.Random.Shuffle(registrationsArr.Append(matched)));

                // Act
                MessageProcessingResult result = await router.RouteMessageAsync(message, context);

                // Assert
                return router.ShouldRouteThrough(matched, result);

            }).QuickCheckThrowOnFailure();
        }

        private TestMessageRouter CreateMessageRouter(
            IEnumerable<TestMessageHandlerRegistration> registrations,
            Action<MessageRouterOptions> configureOptions = null,
            [CallerMemberName] string memberName = null)
        {
            return TestMessageRouter.Create(registrations.ToArray(), _outputWriter, configureOptions, memberName);
        }
    }
}
