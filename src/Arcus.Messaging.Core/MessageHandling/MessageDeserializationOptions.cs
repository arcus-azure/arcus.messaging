using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the consumer-configurable options, as part of the <see cref="MessageRouterOptions"/>, to change the behavior of the <see cref="MessageRouter"/>.
    /// </summary>
    public class MessageDeserializationOptions
    {
        private AdditionalMemberHandling _additionalMembers = AdditionalMemberHandling.Error;

        /// <summary>
        /// Gets the configured JSON options that are the result of setting the <see cref="AdditionalMembers"/>.
        /// </summary>
        internal JsonSerializerOptions JsonOptions { get; private set; } = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

        /// <summary>
        /// Gets or sets the flag indicating whether or not the default JSON deserialization should ignore missing members
        /// when trying to match a incoming message with a message handler's message type.
        /// </summary>
        public AdditionalMemberHandling AdditionalMembers
        {
            get => _additionalMembers;
            set
            {
                _additionalMembers = value;
                JsonOptions = new JsonSerializerOptions
                {
                    UnmappedMemberHandling = value switch
                    {
                        AdditionalMemberHandling.Error => JsonUnmappedMemberHandling.Disallow,
                        AdditionalMemberHandling.Ignore => JsonUnmappedMemberHandling.Skip,
                        _ => JsonUnmappedMemberHandling.Disallow
                    }
                };
            }
        }
    }
}