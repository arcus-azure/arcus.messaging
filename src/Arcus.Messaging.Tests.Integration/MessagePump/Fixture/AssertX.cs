using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    /// <summary>
    /// Additional specific assert functionality  that aren't by default supported by xUnit.
    /// </summary>
    public static class AssertX
    {
        public static void RetryAssertUntil(Action assertion, TimeSpan timeout, ILogger logger)
        {
            RetryPolicy retryPolicy =
                Policy.Handle<Exception>(exception =>
                      {
                          logger.LogError(exception, "Failed assertion. Reason: {Message}", exception.Message);
                          return true;
                      })
                      .WaitAndRetryForever(index => TimeSpan.FromSeconds(5));

            Policy.Timeout(timeout)
                  .Wrap(retryPolicy)
                  .Execute(assertion);
        }

        public static RequestTelemetry GetRequestFrom(
            IEnumerable<ITelemetry> telemetries,
            Predicate<RequestTelemetry> filter)
        {
            Assert.NotEmpty(telemetries);

            ITelemetry[] result = telemetries.Where(t => t is RequestTelemetry r && filter(r)).ToArray();
            Assert.True(result.Length > 0, "Should find at least a single request telemetry, but got none");

            return (RequestTelemetry) result.First();
        }

        public static DependencyTelemetry GetDependencyFrom(
            IEnumerable<ITelemetry> telemetries,
            Predicate<DependencyTelemetry> filter)
        {
            Assert.NotEmpty(telemetries);

            ITelemetry[] result = telemetries.Where(t => t is DependencyTelemetry r && filter(r)).ToArray();
            Assert.True(result.Length > 0, "Should find at least a single dependency telemetry, but got none");

            return (DependencyTelemetry) result.First();
        }
    }
}
