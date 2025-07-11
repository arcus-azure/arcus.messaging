using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test message model implementation as indication to handle a specific messages in the <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
    /// </summary>
    public class TestMessage
    {
        /// <summary>
        /// Gets or sets the test property.
        /// </summary>
        public string TestProperty { get; set; }
    }
}