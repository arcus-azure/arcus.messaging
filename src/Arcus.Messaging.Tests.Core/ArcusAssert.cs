using System.Collections.Generic;
using Xunit;

namespace Arcus.Messaging.Tests.Core
{
    public static class ArcusAssert
    {
        /// <summary>
        ///     Asserts if a dictionary entry matches the expected value
        /// </summary>
        /// <param name="dictionaryKey">Key in dictionary to check</param>
        /// <param name="expectedValue">Value that is being expected in dictionary</param>
        /// <param name="properties">Dictionary with all entries</param>
        public static void MatchesDictionaryEntry(string dictionaryKey, string expectedValue,
            IDictionary<string, object> properties)
        {
            var foundValue = properties[dictionaryKey];
            Assert.Equal(expectedValue, foundValue);
        }
    }
}