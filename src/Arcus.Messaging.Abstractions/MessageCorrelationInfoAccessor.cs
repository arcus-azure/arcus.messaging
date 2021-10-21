using System;
using Arcus.Observability.Correlation;
using GuardNet;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents an <see cref="ICorrelationInfoAccessor{TCorrelationInfo}"/> implementation that's using the marker <see cref="IMessageCorrelationInfoAccessor"/> interface
    /// for accessing the messaging <see cref="MessageCorrelationInfo"/>.
    /// </summary>
    public class MessageCorrelationInfoAccessor : IMessageCorrelationInfoAccessor
    {
        private readonly ICorrelationInfoAccessor<MessageCorrelationInfo> _implementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfoAccessor" /> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementation"/> is <c>null</c>.</exception>
        public MessageCorrelationInfoAccessor(ICorrelationInfoAccessor<MessageCorrelationInfo> implementation)
        {
            Guard.NotNull(implementation, nameof(implementation), "Requires an implementation of the correlation info accessor using the messaging correlation information");
            _implementation = implementation;
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
