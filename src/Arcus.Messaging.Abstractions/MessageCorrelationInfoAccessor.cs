using System;
using Arcus.Observability.Correlation;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents an <see cref="ICorrelationInfoAccessor{TCorrelationInfo}"/> implementation that's using the marker <see cref="IMessageCorrelationInfoAccessor"/> interface
    /// for accessing the messaging <see cref="MessageCorrelationInfo"/>.
    /// </summary>
    [Obsolete("Will be moved in v3.0 outside the 'Abstractions' library in a separate Serilog-specific library, see the v3.0 migration guide for more information")]
    public class MessageCorrelationInfoAccessor : IMessageCorrelationInfoAccessor
    {
        private readonly ICorrelationInfoAccessor<MessageCorrelationInfo> _implementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfoAccessor" /> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementation"/> is <c>null</c>.</exception>
        public MessageCorrelationInfoAccessor(ICorrelationInfoAccessor<MessageCorrelationInfo> implementation)
        {
            _implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public MessageCorrelationInfo GetCorrelationInfo()
        {
            return _implementation.GetCorrelationInfo();
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        public void SetCorrelationInfo(MessageCorrelationInfo correlationInfo)
        {
            _implementation.SetCorrelationInfo(correlationInfo);
        }
    }
}
