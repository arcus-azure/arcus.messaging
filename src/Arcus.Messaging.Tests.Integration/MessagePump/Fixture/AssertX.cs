using System;
using System.Collections.Generic;
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
            return (RequestTelemetry) Assert.Single(telemetries, t => t is RequestTelemetry r && filter(r));
        }

        public static DependencyTelemetry GetDependencyFrom(
            IEnumerable<ITelemetry> telemetries,
            Predicate<DependencyTelemetry> filter)
        {
            return (DependencyTelemetry) Assert.Single(telemetries, t => t is DependencyTelemetry r && filter(r));
        }
    }
}
