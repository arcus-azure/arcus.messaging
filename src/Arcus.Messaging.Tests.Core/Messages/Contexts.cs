using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Messaging.Abstractions;
using Bogus;

namespace Arcus.Messaging
{
    public static class Contexts
    {
        private static readonly Faker Bogus = new();

        public static MessageContext Any
        {
            get
            {
                string jobId = Bogus.Random.Guid().ToString();
                string messageId = Bogus.Random.Guid().ToString();
                var properties =
                    Bogus.Make(Bogus.Random.Int(1, 5), () => new KeyValuePair<string, object>(Bogus.Random.Guid().ToString(), Bogus.Lorem.Word()))
                         .ToDictionary(item => item.Key, item => item.Value);

                return Bogus.Random.Int(1, 2) switch
                {
                    1 => new DefaultMessageContext(messageId, jobId, properties),
                    2 => new TransactionalMessageContext(messageId, jobId, properties)
                };
            }
        }
    }

    public class DefaultMessageContext(string messageId, string jobId, IDictionary<string, object> properties) : MessageContext(messageId, jobId, properties);

    public class TransactionalMessageContext(string messageId, string jobId, IDictionary<string, object> properties) : MessageContext(messageId, jobId, properties)
    {
        public string TransactionId { get; } = Guid.NewGuid().ToString();
    }
}
