using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a set of <see cref="IMessageHandler{TMessage,TMessageContext}"/> implementations used throughout the message routing tests.
    /// </summary>
    internal static class Handlers
    {
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Creates a message handler that matches against a certain message and context type.
        /// </summary>
        internal static TestMessageHandlerRegistration Matched<TMessage, TMessageContext>(TMessage message, TMessageContext context)
            where TMessageContext : MessageContext
        {
            return Messages.Match(message,
                (FreshPizzaOrdered _) => Contexts.Match(context,
                    (DefaultMessageContext _) => MatchedCore<FreshPizzaOrdered, DefaultMessageContext>(),
                    (TransactionalMessageContext _) => MatchedCore<FreshPizzaOrdered, TransactionalMessageContext>(),
                    (SpecializedMessageContext _) => MatchedCore<FreshPizzaOrdered, SpecializedMessageContext>()),
                (HealthyTreePlanted _) => Contexts.Match(context,
                    (DefaultMessageContext _) => MatchedCore<HealthyTreePlanted, DefaultMessageContext>(),
                    (TransactionalMessageContext _) => MatchedCore<HealthyTreePlanted, TransactionalMessageContext>(),
                    (SpecializedMessageContext _) => MatchedCore<HealthyTreePlanted, SpecializedMessageContext>()),
                (NewStarDiscovered _) => Contexts.Match(context,
                    (DefaultMessageContext _) => MatchedCore<NewStarDiscovered, DefaultMessageContext>(),
                    (TransactionalMessageContext _) => MatchedCore<NewStarDiscovered, TransactionalMessageContext>(),
                    (SpecializedMessageContext _) => MatchedCore<NewStarDiscovered, SpecializedMessageContext>()));

            TestMessageHandlerRegistration MatchedCore<T, TContext>() where TContext : MessageContext
            {
                return Bogus.Random.Bool() ? Success<T, TContext>() : FailedT<T, TContext>();
            }
        }

        /// <summary>
        /// Creates a message handler that always succeeds against a certain message and context type.
        /// </summary>
        internal static TestMessageHandlerRegistration Success<TMessage, TMessageContext>(
            Action<TestMessageHandlerOptions<TMessage, TMessageContext>> configureOptions = null)
            where TMessageContext : MessageContext
        {
            return TestMessageHandlerRegistration.Create((provider, handlerId) =>
            {
                return new MatchedMessageHandler<TMessage, TMessageContext>(handlerId, provider.GetRequiredService<MessageHandlerTriggerHistory>());

            }, (TestMessageHandlerOptions<TMessage, TMessageContext> options) =>
            {
                configureOptions?.Invoke(options);

                if (Bogus.Random.Bool())
                {
                    options.AddMessageBodyFilter(_ => true);
                }

                if (Bogus.Random.Bool())
                {
                    options.AddMessageContextFilter(_ => true);
                }
            });
        }

        /// <summary>
        /// Creates a message handler that always fails against a random picked message and context type.
        /// </summary>
        internal static TestMessageHandlerRegistration Failed()
        {
            return Bogus.Random.Int(1, 3) switch
            {
                1 => FailedT<NewStarDiscovered, DefaultMessageContext>(),
                2 => FailedT<FreshPizzaOrdered, TestMessageContext>(),
                3 => FailedT<HealthyTreePlanted, SpecializedMessageContext>()
            };
        }

        /// <summary>
        /// Creates a message handler that always fails against a message and context type.
        /// </summary>
        internal static TestMessageHandlerRegistration FailedT<TMessage, TMessageContext>() where TMessageContext : MessageContext
        {
            return TestMessageHandlerRegistration.Create<TMessage, TMessageContext, FailedMessageHandler<TMessage, TMessageContext>>((provider, handlerId) =>
            {
                return new(handlerId, provider.GetRequiredService<MessageHandlerTriggerHistory>());
            });
        }

        /// <summary>
        /// Creates a message handler that always does not match against a random picked message and context type.
        /// </summary>
        internal static TestMessageHandlerRegistration Unrelated()
        {
            return Bogus.Random.Int(1, 3) switch
            {
                1 => Unrelated<FreshPizzaOrdered, DefaultMessageContext>(),
                2 => Unrelated<HealthyTreePlanted, TestMessageContext>(),
                3 => Unrelated<NewStarDiscovered, SpecializedMessageContext>()
            };
        }

        /// <summary>
        /// Creates a message handler that always does not match against a message and context type.
        /// </summary>
        internal static TestMessageHandlerRegistration Unrelated<TMessage, TMessageContext>() where TMessageContext : MessageContext
        {
            return TestMessageHandlerRegistration.Create<TMessage, TMessageContext, UnrelatedMessageHandler<TMessage, TMessageContext>>((provider, handlerId) =>
            {
                return new(handlerId, provider.GetRequiredService<MessageHandlerTriggerHistory>());

            }, IgnoreOptions);
        }

        private static void IgnoreOptions<TMessage, TMessageContext>(TestMessageHandlerOptions<TMessage, TMessageContext> options)
            where TMessageContext : MessageContext
        {
            if (Bogus.Random.Bool())
            {
                (bool? matchBody, bool? matchContext) = Bogus.PickRandom<(bool?, bool?)>(
                    (null, null),
                    (null, false),
                    (null, true),
                    (false, null),
                    (false, false),
                    (false, true),
                    (true, null),
                    (true, false));

                options.AddMessageBodyFilter(msg => matchBody ?? throw new InvalidOperationException("[Test] sabotage message body filter"));
                options.AddMessageContextFilter(ctx => matchContext ?? throw new InvalidOperationException("[Test] sabotage message context filter"));
            }
            else
            {
                options.UseMessageBodyDeserializer(new SabotageMessageBodyDeserializer());
            }
        }

        private sealed class SabotageMessageBodyDeserializer : IMessageBodyDeserializer
        {
            public Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody)
            {
                return Task.FromResult(Bogus.Random.Bool()
                    ? MessageBodyResult.Failure("cannot deserialize this")
                    : MessageBodyResult.Success(new SabotageMessageBody()));
            }

            private sealed class SabotageMessageBody;
        }
    }

    public class MatchedMessageHandler<T, TContext>(string handlerId, MessageHandlerTriggerHistory history) : IMessageHandler<T, TContext> where TContext : MessageContext
    {
        public Task ProcessMessageAsync(T message, TContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
        {
            history.AddsOccurrence(typeof(MatchedMessageHandler<T, TContext>), handlerId);
            return Task.CompletedTask;
        }
    }

    public class UnrelatedMessageHandler<T, TContext>(string handlerId, MessageHandlerTriggerHistory history) : IMessageHandler<T, TContext> where TContext : MessageContext
    {
        public Task ProcessMessageAsync(T message, TContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
        {
            history.AddsOccurrence(typeof(UnrelatedMessageHandler<T, TContext>), handlerId);
            return Task.CompletedTask;
        }
    }

    public class FailedMessageHandler<T, TContext>(string handlerId, MessageHandlerTriggerHistory history) : IMessageHandler<T, TContext> where TContext : MessageContext
    {
        public Task ProcessMessageAsync(T message, TContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
        {
            history.AddsOccurrence(typeof(FailedMessageHandler<T, TContext>), handlerId);
            throw new InvalidOperationException($"Sabotage message processing for message '{nameof(T)}'!");
        }
    }

    public class MessageHandlerTriggerHistory
    {
        private readonly Collection<(string handlerId, Type handlerType)> _firedMessageHandlers = [];

        public void AddsOccurrence(Type messageHandlerType, string handlerId)
        {
            _firedMessageHandlers.Add((handlerId, messageHandlerType));
        }

        internal bool IsFired(TestMessageHandlerRegistration registration)
        {
            return _firedMessageHandlers.Contains((registration.HandlerId, registration.MessageHandlerType));
        }
    }
}
