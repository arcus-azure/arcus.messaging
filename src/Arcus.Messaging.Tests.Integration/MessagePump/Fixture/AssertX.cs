using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    /// <summary>
    /// Additional specific assert functionality  that aren't by default supported by xUnit.
    /// </summary>
    public static class AssertX
    {
        public static T Any<T>(IEnumerable<T> collection, Action<T> action)
        {
            Stack<(int index, object item, Exception exception)> failures = new();
            T[] array = collection.ToArray();
            
            for (int index = 0; index < array.Length; ++index)
            {
                T item = array[index];
                try
                {
                    action(item);
                    return item;
                }
                catch (Exception ex)
                {
                    failures.Push((index, item, ex));
                }
            }

            throw new XunitException(
                $"None of the {array.Length} item(s) matches against the given action: {Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, failures.Select(f => $"- [{f.index}] {f.item}: {f.exception}"))}");
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
