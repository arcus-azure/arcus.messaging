using System;
using Arcus.Observability.Correlation;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents a marker interface for an <see cref="ICorrelationInfoAccessor{TCorrelationInfo}"/> implementation using the messaging <see cref="MessageCorrelationInfo"/>.
    /// </summary>
    /// <seealso cref="ICorrelationInfoAccessor{TCorrelationInfo}"/>
    [Obsolete("Will be moved in v3.0 outside the 'Abstractions' library in a separate Serilog-specific library, see the v3.0 migration guide for more information")]
    public interface IMessageCorrelationInfoAccessor : ICorrelationInfoAccessor<MessageCorrelationInfo>
    {
    }
}