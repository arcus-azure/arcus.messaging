using System;
using System.Threading.Tasks;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a set of available <see cref="IMessageBodyDeserializer"/> implementations used throughout message routing tests.
    /// </summary>
    internal static class Deserializers
    {
        internal static IMessageBodyDeserializer ForResult(object body)
        {
            return new StubParsing(body);
        }

        internal sealed class StubParsing(object body) : IMessageBodyDeserializer
        {
            public Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody)
            {
                return Task.FromResult(MessageBodyResult.Success(body));
            }
        }

        /// <summary>
        /// Creates a deserializer that parses always to a certain message of type <typeparamref name="T"/>.
        /// </summary>
        internal static IMessageBodyDeserializer ForMessage<T>()
        {
            return new ParseAny<T>();
        }

        private sealed class ParseAny<T> : IMessageBodyDeserializer
        {
            public Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody)
            {
                return Task.FromResult(MessageBodyResult.Success(Messages.Create<T>()));
            }
        }

        internal static IMessageBodyDeserializer Failed => new AlwaysFail();

        private sealed class AlwaysFail : IMessageBodyDeserializer
        {
            public Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody)
            {
                return Task.FromResult(MessageBodyResult.Failure("sabotage message body deserialization"));
            }
        }
    }
}
