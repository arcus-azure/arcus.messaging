using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Messaging.Abstractions;
using Bogus;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a set of example <see cref="MessageContext"/> implementations used throughout message routing tests.
    /// </summary>
    public static class Contexts
    {
        private static readonly Faker Bogus = new();

        private static string JobId => Bogus.Random.Guid().ToString();
        private static string MessageId => Bogus.Random.Guid().ToString();

        private static Dictionary<string, object> Properties
        {
            get
            {
                var properties =
                    Bogus.Make(Bogus.Random.Int(1, 5), () => new KeyValuePair<string, object>(Bogus.Random.Guid().ToString(), Bogus.Lorem.Word()))
                         .ToDictionary(item => item.Key, item => item.Value);

                return properties;
            }
        }

        /// <summary>
        /// Gets a default, a.k.a. minimal, context implementation.
        /// </summary>
        public static DefaultMessageContext Default => new(MessageId, JobId, Properties);

        /// <summary>
        /// Gets a context implementation with a simple extra <see cref="TransactionalMessageContext.TransactionId"/> property.
        /// </summary>
        public static TransactionalMessageContext Transactional => new(MessageId, JobId, Properties);

        /// <summary>
        /// Gets a context implementation with more complex <see cref="SpecializedMessageContext.MedaData"/> property.
        /// </summary>
        public static SpecializedMessageContext Specialized => new(Properties, MessageId, JobId, Properties);

        /// <summary>
        /// Gets any kind of custom <see cref="MessageContext"/> implementation.
        /// </summary>
        public static MessageContext Any => Bogus.Random.Int(1, 3) switch
        {
            1 => Default,
            2 => Transactional,
            3 => Specialized
        };

        /// <summary>
        /// Determine the custom type of the given <paramref name="context"/>.
        /// </summary>
        public static Type GetContextTypeName(MessageContext context)
        {
            return context switch
            {
                DefaultMessageContext _ => typeof(DefaultMessageContext),
                TransactionalMessageContext _ => typeof(TransactionalMessageContext),
                SpecializedMessageContext _ => typeof(SpecializedMessageContext),
                _ => throw new ArgumentOutOfRangeException(nameof(context), context.GetType(), $"Unknown message context type '{context.GetType().FullName}'")
            };
        }

        /// <summary>
        /// Provides a pattern matching function based on the available type of the given <paramref name="context"/>.
        /// </summary>
        public static TResult Match<TResult>(
            MessageContext context,
            Func<DefaultMessageContext, TResult> isDefault,
            Func<TransactionalMessageContext, TResult> isTransactional,
            Func<SpecializedMessageContext, TResult> isSpecialized)
        {
            return context switch
            {
                DefaultMessageContext ctx => isDefault(ctx),
                TransactionalMessageContext ctx => isTransactional(ctx),
                SpecializedMessageContext ctx => isSpecialized(ctx),
                _ => throw new ArgumentException($"Unknown message context type '{context.GetType().FullName}'", nameof(context))
            };
        }
    }

    public class DefaultMessageContext(string messageId, string jobId, IDictionary<string, object> properties) : MessageContext(messageId, jobId, properties);

    public class TransactionalMessageContext(string messageId, string jobId, IDictionary<string, object> properties) : MessageContext(messageId, jobId, properties)
    {
        public string TransactionId { get; } = Guid.NewGuid().ToString();
    }

    public class SpecializedMessageContext(IReadOnlyDictionary<string, object> metaData, string messageId, string jobId, IDictionary<string, object> properties) : MessageContext(messageId, jobId, properties)
    {
        public IReadOnlyDictionary<string, object> MedaData { get; } = metaData;
    }
}
