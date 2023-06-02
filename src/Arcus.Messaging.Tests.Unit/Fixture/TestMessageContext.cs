using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Bogus;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test <see cref="MessageContext"/> implementation as indication to handle specific contexts in the <see cref="IMessageHandler{TMessage}"/>.
    /// </summary>
    [DebuggerStepThrough]
    public class TestMessageContext : MessageContext
    {
        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="messageId">Unique identifier of the message</param>
        /// <param name="properties">Contextual properties provided on the message</param>
        public TestMessageContext(string messageId, IDictionary<string, object> properties) : base(messageId, "job-id", properties) { }

        /// <summary>
        /// Generate a new <see cref="TestMessageContext"/> instance with random values.
        /// </summary>
        public static TestMessageContext Generate()
        {
            IEnumerable<string> keys = BogusGenerator.Random.WordsArray(5, 10).Distinct();
            byte[] values = BogusGenerator.Random.Bytes(keys.Count());
            Dictionary<string, object> properties = keys.Zip(values).ToDictionary(item => item.First, item => (object) item.Second);

            return new TestMessageContext(
                messageId: Guid.NewGuid().ToString(),
                properties: properties);
        }
    }
}