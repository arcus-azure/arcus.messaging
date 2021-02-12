using System.Collections;
using System.Collections.Generic;

namespace Arcus.Messaging.Tests.Unit
{
    /// <summary>
    /// Represents a collection of blank inputs to test against argument guards.
    /// </summary>
    public class Blanks : IEnumerable<object[]>
    {
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] {null};
            yield return new object[] {""};
            yield return new object[] {" "};
            yield return new object[] {"     "};
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
