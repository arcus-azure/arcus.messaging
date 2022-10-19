using System;
using System.Collections.Generic;
using System.Text.Json;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Functions.Worker
{
#if NET6_0
    /// <summary>
    /// Extensions on the <see cref="FunctionContext"/> related to message correlation.
    /// </summary>
    public static class FunctionContextExtensions
    {
        /// <summary>
        /// Determines the W3C message correlation based on the 'UserProperties' binding data in the <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The execution context of the executed isolated Azure Function.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> is <c>null</c> or doesn't contain any binding data in its binding context.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no 'UserProperties' binding data can be found in the <paramref name="context"/>.</exception>
        public static MessageCorrelationResult GetCorrelationInfo(this FunctionContext context)
        {
            Guard.NotNull(context, nameof(context), "Requires a function context instance to retrieve the message correlation");
            Guard.NotNull(context.BindingContext, nameof(context), "Requires a function context instance with a binding context to retrieve the message correlation");
            Guard.NotNull(context.BindingContext.BindingData, nameof(context), "Requires a function context with a binding data to retrieve the message correlation");

            return GetCorrelationInfo(context, MessageCorrelationFormat.W3C);
        }

        /// <summary>
        /// Determines the message correlation based on the 'UserProperties' binding data in the <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The execution context of the executed isolated Azure Function.</param>
        /// <param name="correlationFormat">The type of correlation format that should be used to find the necessary correlation information in the 'UserProperties'.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> is <c>null</c> or doesn't contain any binding data in its binding context.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no 'UserProperties' binding data can be found in the <paramref name="context"/>.</exception>
        public static MessageCorrelationResult GetCorrelationInfo(this FunctionContext context, MessageCorrelationFormat correlationFormat)
        {
            Guard.NotNull(context, nameof(context), "Requires a function context instance to retrieve the message correlation");
            Guard.NotNull(context.BindingContext, nameof(context), "Requires a function context instance with a binding context to retrieve the message correlation");
            Guard.NotNull(context.BindingContext.BindingData, nameof(context), "Requires a function context with a binding data to retrieve the message correlation");

            IDictionary<string, object> userPropertiesJson = GetUserPropertiesJson(context);
            switch (correlationFormat)
            {
                case MessageCorrelationFormat.W3C: 
                    return GetCorrelationInfoViaW3C(context, userPropertiesJson);
                case MessageCorrelationFormat.Hierarchical: 
                    return GetCorrelationInfoViaHierarchical(userPropertiesJson);
                default:
                    throw new ArgumentOutOfRangeException(nameof(correlationFormat), correlationFormat, "Unknown message correlation format");
            }
        }

        private static MessageCorrelationResult GetCorrelationInfoViaW3C(FunctionContext context, IDictionary<string, object> userPropertiesJson)
        {
            (string transactionIdW3C, string operationParentIdW3C) = userPropertiesJson.GetTraceParent();
            var clientForExistingParent = context.InstanceServices.GetRequiredService<TelemetryClient>();

            return MessageCorrelationResult.Create(clientForExistingParent, transactionIdW3C, operationParentIdW3C);
        }


        /// <summary>
        /// Determines the Hierarchical message correlation based on the 'UserProperties' binding data in the <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The execution context of the executed isolated Azure Function.</param>
        /// <param name="transactionIdPropertyName">The custom property name to retrieve the transaction ID from the 'UserProperties' binding data.</param>
        /// <param name="operationParentIdPropertyName">The custom property name to retrieve the operation parent ID from the 'UserProperties' binding data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> is <c>null</c> or doesn't contain any binding data in its binding context.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no 'UserProperties' binding data can be found in the <paramref name="context"/>.</exception>
        public static MessageCorrelationResult GetCorrelationInfo(
            this FunctionContext context,
            string transactionIdPropertyName,
            string operationParentIdPropertyName)
        {
            Guard.NotNull(context, nameof(context), "Requires a function context instance to retrieve the message correlation");
            Guard.NotNull(context.BindingContext, nameof(context), "Requires a function context instance with a binding context to retrieve the message correlation");
            Guard.NotNull(context.BindingContext.BindingData, nameof(context), "Requires a function context with a binding data to retrieve the message correlation");
            Guard.NotNullOrWhitespace(transactionIdPropertyName, nameof(transactionIdPropertyName), "Requires a non-blank property name to retrieve the transaction ID from the binding data");
            Guard.NotNullOrWhitespace(operationParentIdPropertyName, nameof(operationParentIdPropertyName), "Requires a non-blank property name to retrieve the operation parent ID from the binding data");

            IDictionary<string, object> userPropertiesJson = GetUserPropertiesJson(context);
            return GetCorrelationInfoViaHierarchical(userPropertiesJson, transactionIdPropertyName, operationParentIdPropertyName);

        }

        private static MessageCorrelationResult GetCorrelationInfoViaHierarchical(
            IDictionary<string, object> userPropertiesJson,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId)
        {
            userPropertiesJson.TryGetValue(transactionIdPropertyName, out object transactionIdHierarchicalValue);
            userPropertiesJson.TryGetValue(operationParentIdPropertyName, out object operationParentIdHierarchical);
            var operationId = Guid.NewGuid().ToString();
            var transactionId = transactionIdHierarchicalValue?.ToString() ?? Guid.NewGuid().ToString();

            var correlationInfo = new MessageCorrelationInfo(operationId, transactionId, operationParentIdHierarchical?.ToString());
            return MessageCorrelationResult.Create(correlationInfo);
        }

        private static IDictionary<string, object> GetUserPropertiesJson(FunctionContext context)
        {
            if (context.BindingContext.BindingData.TryGetValue("UserProperties", out object userPropertiesObject)
                && userPropertiesObject != null)
            {
                string userPropertiesJson = userPropertiesObject.ToString();

                return JsonSerializer.Deserialize<Dictionary<string, object>>(userPropertiesJson);
            }
            
            
            throw new InvalidOperationException(
                "Cannot determine message correlation because function context does not contain any 'UserProperties' binding data");
        }
    }
#endif
}
