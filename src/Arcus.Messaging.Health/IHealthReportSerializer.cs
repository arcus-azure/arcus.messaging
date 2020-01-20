using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Arcus.Messaging.Health
{
    /// <summary>
    /// Represents a custom serialization for the <see cref="HealthReport"/> model.
    /// </summary>
    public interface IHealthReportSerializer
    {
        /// <summary>
        /// Serializes the given <paramref name="healthReport"/> to a series of bytes.
        /// </summary>
        /// <param name="healthReport">The report to serialize.</param>
        byte[] Serialize(HealthReport healthReport);
    }
}
