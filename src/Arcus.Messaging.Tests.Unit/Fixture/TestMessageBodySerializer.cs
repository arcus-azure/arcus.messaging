using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using GuardNet;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test <see cref="IMessageBodySerializer"/> implementation to return a static <see cref="TestMessage"/> instance.
    /// </summary>
    public class TestMessageBodySerializer : IMessageBodySerializer
    {
        private readonly string _expectedExpectedBody;
        private readonly TestMessage _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageBodySerializer"/> class.
        /// </summary>
        public TestMessageBodySerializer() : this(null, new TestMessage())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageBodySerializer"/> class.
        /// </summary>
        public TestMessageBodySerializer(string expectedBody) : this(expectedBody, new TestMessage())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageBodySerializer"/> class.
        /// </summary>
        public TestMessageBodySerializer(string expectedBody, TestMessage message)
        {
            Guard.NotNull(message, nameof(message), "Requires a test message to be used as result of the message deserialization");

            _expectedExpectedBody = expectedBody;
            _message = message;
        }

        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        public Task<MessageResult> DeserializeMessageAsync(string messageBody)
        {
            Assert.Equal(_expectedExpectedBody, messageBody);
            return Task.FromResult(MessageResult.Success(_message));
        }
    }
}
