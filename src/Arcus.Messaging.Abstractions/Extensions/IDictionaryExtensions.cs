using System.Collections.Generic;
using System.Diagnostics;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    /// Extensions on the <see cref="IDictionary{TKey,TValue}"/> type (which represents the Azure Service Bus application properties) to retrieve message correlation information.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IDictionaryExtensions
    {
        /// <summary>
        /// Gets the deconstructed W3C correlation 'traceparent' from the <paramref name="applicationProperties"/>.
        /// </summary>
        /// <param name="applicationProperties">The application properties of the received message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="applicationProperties"/> is <c>null</c>.</exception>
        public static (string transactionId, string operationParentId) GetTraceParent(
            this IReadOnlyDictionary<string, object> applicationProperties)
        {
            if (applicationProperties is null)
            {
                throw new ArgumentNullException(nameof(applicationProperties));
            }

            return GetTraceParent((IDictionary<string, object>) applicationProperties);
        }

         /// <summary>
        /// Gets the deconstructed W3C correlation 'traceparent' from the <paramref name="applicationProperties"/>.
        /// </summary>
        /// <param name="applicationProperties">The application properties of the received message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="applicationProperties"/> is <c>null</c>.</exception>
        public static (string transactionId, string operationParentId) GetTraceParent(this IDictionary<string, object> applicationProperties)
        {
            if (applicationProperties is null)
            {
                throw new ArgumentNullException(nameof(applicationProperties));
            }

            if (applicationProperties.TryGetValue("Diagnostic-Id", out object value)
                && value != null)
            {
                string traceParent = TruncateString(value.ToString(), 55);
                if (IsTraceParentHeaderW3CCompliant(traceParent))
                {
                    return DeconstructPotentialTraceParent(traceParent);
                }
            }

            string transactionId = ActivityTraceId.CreateRandom().ToHexString();
            string operationParentId = ActivitySpanId.CreateRandom().ToHexString();

            return (transactionId, operationParentId);
        }

        private static (string transactionId, string operationParentId) DeconstructPotentialTraceParent(string traceParent)
        {
            string transactionId = ActivityTraceId.CreateFromString(traceParent.AsSpan(3, 32)).ToHexString();
            string operationParentId = ActivitySpanId.CreateFromString(traceParent.AsSpan(36, 16)).ToHexString();

            return (transactionId, operationParentId);
        }

        private static string TruncateString(string input, int maxLength)
        {
            if (input != null && input.Length > maxLength)
            {
                input = input.Substring(0, maxLength);
            }

            return input;
        }

        private static bool IsTraceParentHeaderW3CCompliant(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            bool firstPartOutsideBoundaries = 
                (id[0] < '0' || id[0] > '9') && (id[0] < 'a' || id[0] > 'f');

            bool secondPartOutsideBoundaries = 
                (id[1] < '0' || id[1] > '9') && (id[1] < 'a' || id[1] > 'f');

            if (id.Length != 55 
                || firstPartOutsideBoundaries 
                || secondPartOutsideBoundaries)
            {
                return false;
            }

            return id[0] != 'f' || id[1] != 'f';
        }
    }
}
