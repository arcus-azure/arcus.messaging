using Arcus.Observability.Correlation;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents a marker interface for an <see cref="ICorrelationInfoAccessor{TCorrelationInfo}"/> implementation using the messaging <see cref="MessageCorrelationInfo"/>.
    /// </summary>
    /// <seealso cref="ICorrelationInfoAccessor{TCorrelationInfo}"/>
    public interface IMessageCorrelationInfoAccessor : ICorrelationInfoAccessor<MessageCorrelationInfo>
    {
    }
}