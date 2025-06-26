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
    /// Test <see cref="MessageContext"/> implementation as indication to handle specific contexts in the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
    /// </summary>
    [DebuggerStepThrough]
    public class TestMessageContext : MessageContext
    {
        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="properties">The contextual properties provided on the message.</param>
        public TestMessageContext(string messageId, IDictionary<string, object> properties) : this(messageId, "job-id", properties) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">The unique ID to identify the registered message handlers that can handle this context.</param>
        /// <param name="properties">The contextual properties provided on the message.</param>
        public TestMessageContext(string messageId, string jobId, IDictionary<string, object> properties) : base(messageId, jobId, properties) { }

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