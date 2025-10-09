using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging
{
    public static class Handlers
    {
        private static readonly Faker Bogus = new();

        public static Action<MessageHandlerCollection> AlwaysFailed()
        {
            return Bogus.Random.Int(1, 3) switch
            {
                1 => AlwaysFailedT<FreshPizzaOrdered, DefaultMessageContext>(),
                2 => AlwaysFailedT<HealthyTreePlanted, TransactionalMessageContext>(),
                3 => AlwaysFailedT<NewStarDiscovered, DefaultMessageContext>()
            };
        }

        public static Action<MessageHandlerCollection> AlwaysFailedT<TMessage, TMessageContext>()
            where TMessageContext : MessageContext
        {
            return collection =>
            {
                collection.Services.AddTransient(provider =>
                {
                    var handler = new AlwaysFailMessageHandler<TMessage, TMessageContext>(provider.GetRequiredService<MessageHandlerTriggerHistory>());
                    var logger = provider.GetRequiredService<ILogger<AlwaysFailMessageHandler<TMessage, TMessageContext>>>();

                    return MessageHandler.Create(handler, logger, collection.JobId);
                });
            };
        }

        public static MessageHandlerCollection WithMessageHandler<TMessage, TMessageContext>(
            this MessageHandlerCollection collection,
            Func<IServiceProvider, IMessageHandler<TMessage, TMessageContext>> implementationFactory,
            Action<MessageHandlerOptions<TMessage, TMessageContext>> configureOptions = null)
            where TMessageContext : MessageContext
        {
            var options = new MessageHandlerOptions<TMessage, TMessageContext>();
            configureOptions?.Invoke(options);

            collection.Services.AddTransient(
                provider => MessageHandler.Create(
                    implementationFactory(provider),
                    provider.GetRequiredService<ILogger<IMessageHandler<TMessage, TMessageContext>>>(),
                    collection.JobId,
                    options.MessageBodyFilter,
                    options.MessageContextFilter,
                    options.MessageBodySerializer));

            return collection;
        }

        public static MessageHandlerCollection WithFailedMessageHandler<TMessage, TMessageContext>(this MessageHandlerCollection collection)
            where TMessageContext : MessageContext
        {
            collection.Services.AddTransient(
                provider => MessageHandler.Create(
                    new AlwaysFailMessageHandler<TMessage, TMessageContext>(provider.GetRequiredService<MessageHandlerTriggerHistory>()),
                    provider.GetRequiredService<ILogger<AlwaysFailMessageHandler<TMessage, TMessageContext>>>(),
                    collection.JobId));

            return collection;
        }
    }

    public class DefaultMessageHandler<TMessage, TMessageContext>(MessageHandlerTriggerHistory history)
        : IMessageHandler<TMessage, TMessageContext> where TMessageContext : MessageContext
    {
        public Task ProcessMessageAsync(TMessage message, TMessageContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
        {
            history.AddsOccurrence(typeof(AlwaysFailMessageHandler<TMessage, TMessageContext>));
            return Task.CompletedTask;
        }
    }

    public class AlwaysFailMessageHandler<TMessage, TMessageContext>(MessageHandlerTriggerHistory history)
        : IMessageHandler<TMessage, TMessageContext> where TMessageContext : MessageContext
    {
        public Task ProcessMessageAsync(TMessage message, TMessageContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
        {
            history.AddsOccurrence(typeof(AlwaysFailMessageHandler<TMessage, TMessageContext>));
            throw new InvalidOperationException($"Sabotage message processing for message '{nameof(TMessage)}'!");
        }
    }

    public class MessageHandlerOptions<TMessage, TMessageContext> where TMessageContext : MessageContext
    {
        public Expression<Func<TMessage, bool>> MessageBodyFilter { get; set; }
        public Expression<Func<TMessageContext, bool>> MessageContextFilter { get; set; }
        public IMessageBodySerializer MessageBodySerializer { get; set; }
    }

    public class MessageHandlerTriggerHistory
    {
        private readonly Collection<Type> _firedMessageHandlers = [];

        public void AddsOccurrence(Type messageHandlerType)
        {
            _firedMessageHandlers.Add(messageHandlerType);
        }

        public void ShouldBeEmpty()
        {
            Assert.Empty(_firedMessageHandlers);
        }

        public void ShouldBeFired(Type firedMessageHandler)
        {
            Assert.Contains(firedMessageHandler, _firedMessageHandlers);
        }

        public void ShouldNotBeFired(Type notFiredMessageHandler)
        {
            Assert.DoesNotContain(notFiredMessageHandler, _firedMessageHandlers);
        }
    }
}
