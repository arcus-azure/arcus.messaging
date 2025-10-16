using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Bogus;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Arb = FsCheck.Fluent.Arb;
using Gen = FsCheck.Fluent.Gen;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a possible implementation of the <see cref="MessageRouter"/> to verify general message routing functionality.
    /// </summary>
    internal sealed class TestMessageRouter : MessageRouter
    {
        private readonly IReadOnlyCollection<TestMessageHandlerRegistration> _registrations;
        private readonly MessageHandlerTriggerHistory _triggerHistory;
        private readonly InMemoryLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageRouter"/> class.
        /// </summary>
        private TestMessageRouter(
            IReadOnlyCollection<TestMessageHandlerRegistration> registrations,
            MessageHandlerTriggerHistory triggerHistory,
            IServiceProvider provider,
            MessageRouterOptions options,
            InMemoryLogger logger)
            : base(provider, options, provider.GetService<ILogger<TestMessageRouter>>())
        {
            _registrations = registrations;
            _triggerHistory = triggerHistory;
            _logger = logger;
        }

        /// <summary>
        /// Creates a <see cref="TestMessageRouter"/> based on a set of message handler <paramref name="registrations"/>.
        /// </summary>
        public static TestMessageRouter Create(
            TestMessageHandlerRegistration[] registrations,
            ITestOutputHelper outputWriter,
            Action<MessageRouterOptions> configureOptions = null,
            [CallerMemberName] string memberName = null)
        {
            var options = new MessageRouterOptions();
            configureOptions?.Invoke(options);
            var triggerHistory = new MessageHandlerTriggerHistory();

            var services = new ServiceCollection();
            services.AddSingleton(triggerHistory);

            var logger = new InMemoryLogger();
            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                if (memberName?.EndsWith("Property") is true)
                {
                    logging.AddProvider(new DelegatedLoggerProvider(logger));
                }
                else
                {
                    logging.AddXunitTestLogging(outputWriter);
                }
            });

            Assert.All(registrations, r => services.AddTransient(r.CreateMessageHandler));
            return new(registrations, triggerHistory, services.BuildServiceProvider(), options, logger);
        }

        /// <summary>
        /// Routes the given <paramref name="message"/> model through the registered message handlers.
        /// </summary>
        public async Task<MessageProcessingResult> RouteMessageAsync(object message, MessageContext messageContext, Encoding messageEncoding = null)
        {
            Type contextType = Contexts.GetContextTypeName(messageContext);
            string registrationDescription = CreateRegistrationsDescription(message?.GetType(), contextType);
            Logger.LogDebug("[Test:Setup] Message '{MessageType}' gets routed in context '{MessageContextType}' through handlers: {HandlersDescription}", message?.GetType().Name, contextType.Name, Environment.NewLine + registrationDescription);

            var messageBody = messageEncoding != null
                ? BinaryData.FromBytes(messageEncoding.GetBytes(JsonSerializer.Serialize(message)))
                : BinaryData.FromObjectAsJson(message);

            var messageCorrelation = new MessageCorrelationInfo("operation-id", "transaction-id");

            var result = await Contexts.Match(messageContext,
                ctx => RouteMessageThroughRegisteredHandlersAsync(ServiceProvider, messageBody, ctx, messageCorrelation, TestContext.Current.CancellationToken),
                ctx => RouteMessageThroughRegisteredHandlersAsync(ServiceProvider, messageBody, ctx, messageCorrelation, TestContext.Current.CancellationToken),
                ctx => RouteMessageThroughRegisteredHandlersAsync(ServiceProvider, messageBody, ctx, messageCorrelation, TestContext.Current.CancellationToken));

            return result;
        }

        private string CreateRegistrationsDescription(Type messageType, Type messageContextType)
        {
            if (_registrations.Count <= 0)
            {
                return "- <none>";
            }

            return _registrations.Select(r =>
            {
                string match =
                    messageType == r.MessageHandlerType.GenericTypeArguments[0]
                    && messageContextType == r.MessageHandlerType.GenericTypeArguments[1]
                        ? "✓" : "✗";

                return $"- {match} {r}";

            }).Aggregate((a, b) => a + Environment.NewLine + b);
        }

        /// <summary>
        /// Verifies that this message router did indeed not fire any registered message handler, producing the given <paramref name="result"/>.
        /// </summary>
        internal Property ShouldNotFoundMatched(MessageProcessingResult result, params string[] errorParts)
        {
            Property failedResult = (result.Error is MessageProcessingError.CannotFindMatchedHandler).Label("should represent a failed result, but wasn't: " + CaptureLogLines());
            Property errorMessage = (!result.IsSuccessful && errorParts.All(part => result.ErrorMessage.Contains(part, StringComparison.OrdinalIgnoreCase))).Label($"should contain the error parts ({string.Join(", ", errorParts)}) in the error message of the failed result: {result}, but not all were: " + CaptureLogLines());

            Property noHandlersFired = _registrations.All(r => !_triggerHistory.IsFired(r)).Label("none of the registered handlers should get fired, but at least one was: " + CaptureLogLines());

            return failedResult.And(errorMessage).And(noHandlersFired);
        }

        /// <summary>
        /// Verifies that the <paramref name="expectedFired"/> message handler was indeed fired by this message router,
        /// producing the given <paramref name="result"/>.
        /// </summary>
        internal Property ShouldRouteThrough(TestMessageHandlerRegistration expectedFired, MessageProcessingResult result)
        {
            TestMessageHandlerRegistration[] firedHandlers = _registrations.Where(r => _triggerHistory.IsFired(r)).ToArray();

            Property singleHandlerMatch =
                (firedHandlers.Count(h => h.MessageHandlerType == expectedFired.MessageHandlerType) == 1)
                .Label($"the expected fired registered message handler is of type '{expectedFired.MessageHandlerType.Name}' was not found in the fired handlers" + CaptureLogLines());

            Property isFailedWhenHandlerIsFailed =
                (IsFailedHandler(expectedFired) == !result.IsSuccessful).Label(
                    "the expected fired registered message handler is a failed one, so the processing result should also be failed, but wasn't" + CaptureLogLines());

            return isFailedWhenHandlerIsFailed.And(singleHandlerMatch);
        }

        private static bool IsFailedHandler(TestMessageHandlerRegistration registration)
        {
            return registration.MessageHandlerType.Name.StartsWith(typeof(FailedMessageHandler<,>).Name);
        }

        private string CaptureLogLines()
        {
            return Environment.NewLine + string.Join(Environment.NewLine, _logger.Logs);
        }
    }

    /// <summary>
    /// Represents the available options on the <see cref="TestMessageHandlerRegistration"/>.
    /// </summary>
    internal class TestMessageHandlerOptions<TMessage, TMessageContext> : MessageHandlerOptions<TMessage, TMessageContext>
        where TMessageContext : MessageContext
    {
        public string JobId { get; set; }

        public void AddFailedMessageBodyFilter() => AddMessageBodyFilter(_ => false);
        public void AddFailedMessageContextFilter() => AddMessageContextFilter(_ => false);

        public void AddMessageBodyFilter(Func<TMessage, bool> bodyFilter) => AddBodyFilter(bodyFilter);
        public void AddMessageContextFilter(Func<TMessageContext, bool> contextFilter) => AddContextFilter(contextFilter);
        public void UseMessageBodyDeserializer(IMessageBodyDeserializer deserializer) => UseBodyDeserializer(_ => deserializer);
    }

    /// <summary>
    /// Represents a single registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> in the application services.
    /// </summary>
    internal sealed class TestMessageHandlerRegistration
    {
        private readonly Func<IServiceProvider, MessageHandler> _implementationFactory;

        internal TestMessageHandlerRegistration(
            Type messageHandlerType,
            string handlerId,
            Func<IServiceProvider, MessageHandler> implementationFactory)
        {
            _implementationFactory = implementationFactory;
            HandlerId = handlerId;
            MessageHandlerType = messageHandlerType;
        }

        internal string HandlerId { get; }
        internal Type MessageHandlerType { get; }

        internal bool OfMessageType(Type messageType) => MessageHandlerType.GenericTypeArguments[0] == messageType || messageType.IsSubclassOf(MessageHandlerType.GenericTypeArguments[0]);

        internal bool OfContextType(Type contextType) => MessageHandlerType.GenericTypeArguments[1] == contextType || contextType.IsSubclassOf(MessageHandlerType.GenericTypeArguments[1]);

        internal static TestMessageHandlerRegistration Create<TMessage, TMessageContext, TMessageHandler>(
                Func<IServiceProvider, string, TMessageHandler> implementationFactory,
                Action<TestMessageHandlerOptions<TMessage, TMessageContext>> configureOptions = null)
                where TMessageContext : MessageContext
                where TMessageHandler : IMessageHandler<TMessage, TMessageContext>
        {
            string handlerId = Guid.NewGuid().ToString();
            return new TestMessageHandlerRegistration(typeof(TMessageHandler), handlerId, serviceProvider =>
            {
                var options = new TestMessageHandlerOptions<TMessage, TMessageContext>();
                configureOptions?.Invoke(options);

                return MessageHandler.Create(
                    sp => implementationFactory(sp, handlerId),
                    options,
                    serviceProvider,
                    options.JobId);
            });
        }

        internal MessageHandler CreateMessageHandler(IServiceProvider provider)
        {
            return _implementationFactory(provider);
        }

        public override string ToString()
        {
            return $"{MessageHandlerType.Name}< {string.Join(", ", MessageHandlerType.GenericTypeArguments.Select(arg => arg.Name))} >";
        }
    }

    /// <summary>
    /// Represents a set of registered <see cref="IMessageHandler{TMessage,TMessageContext}"/> in the application services
    /// which gets passed to the <see cref="TestMessageRouter"/>.
    /// </summary>
    internal sealed class TestMessageHandlerRegistrations : IEnumerable<TestMessageHandlerRegistration>
    {
        private readonly IReadOnlyCollection<TestMessageHandlerRegistration> _registrations;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageHandlerRegistrations"/> class.
        /// </summary>
        public TestMessageHandlerRegistrations(IEnumerable<TestMessageHandlerRegistration> registrations)
        {
            _registrations = registrations.ToArray();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<TestMessageHandlerRegistration> GetEnumerator()
        {
            return _registrations.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal TestMessageHandlerRegistrations ExceptWith(object messageInstance, MessageContext contextInstance)
        {
            Type messageType = messageInstance.GetType();
            Type contextType = contextInstance.GetType();

            return new(_registrations.Except(_registrations.Where(r => r.OfMessageType(messageType) && r.OfContextType(contextType))));
        }

        public static Arbitrary<TestMessageHandlerRegistrations> ArbWithoutMatching => Arb.From(GenWithoutMatching, Shrinker);

        public static Gen<TestMessageHandlerRegistrations> GenWithoutMatching => Gen.Fresh(() =>
        {
            var registrations =
                Bogus.Make(Bogus.Random.Int(5, 10), Handlers.Unrelated)
                     .Concat(Bogus.Make(Bogus.Random.Int(5, 10), Handlers.Failed));

            return new TestMessageHandlerRegistrations(registrations);
        });

        public static Func<TestMessageHandlerRegistrations, IEnumerable<TestMessageHandlerRegistrations>> Shrinker => currentRegistrations =>
        {
            var xs = currentRegistrations.ToList();
            if (xs.Count == 1)
            {
                return [];
            }

            var firstCycle = ShrinkEachItem(xs.Where(IsFailed).ToArray(), xs);
            var secondCycle = ShrinkEachItem(xs.Where(x => IsFailed(x) || IsUnrelated(x)).ToArray(), xs);

            return firstCycle.Concat(secondCycle).Select(regs => new TestMessageHandlerRegistrations(regs));

            IEnumerable<IEnumerable<TestMessageHandlerRegistration>> ShrinkEachItem(
                TestMessageHandlerRegistration[] toBeRemoved,
                IEnumerable<TestMessageHandlerRegistration> current)
            {
                return Enumerable.Range(0, toBeRemoved.Length).Select(index => current.Except(toBeRemoved[..index]));
            }
        };

        private static bool IsFailed(TestMessageHandlerRegistration r) => r.MessageHandlerType.Name.StartsWith("Failed");
        private static bool IsUnrelated(TestMessageHandlerRegistration r) => r.MessageHandlerType.Name.StartsWith("Unrelated");

        public override string ToString()
        {
            if (_registrations.Count <= 0)
            {
                return "- <none>";
            }

            return _registrations.Select(r =>
            {
                return $"- {r.MessageHandlerType.Name}< {string.Join(", ", r.MessageHandlerType.GenericTypeArguments.Select(arg => arg.Name))} >";

            }).Aggregate((a, b) => a + Environment.NewLine + b);
        }
    }

    internal class DelegatedLoggerProvider(ILogger logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }

        public void Dispose()
        {
        }
    }

    internal class InMemoryLogger : ILogger
    {
        internal Collection<string> Logs { get; } = [];

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logs.Add($"{logLevel} > " + formatter(state, exception) + (exception is null ? string.Empty : exception.ToString()));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}
