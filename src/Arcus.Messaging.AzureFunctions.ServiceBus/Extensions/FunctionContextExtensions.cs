using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.ServiceBus.Core;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Functions.Worker
{
#if NET6_0
    /// <summary>
    /// 
    /// </summary>
    public static class FunctionContextExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static MessageCorrelationResult GetCorrelationInfo(this FunctionContext context)
        {
            Guard.NotNull(context, nameof(context), "Requires a function context instance to retrieve the message correlation");
            Guard.NotNull(context.BindingContext, nameof(context), "Requires a function context instance with a binding context to retrieve the message correlation");
            Guard.NotNull(context.BindingContext.BindingData, nameof(context), "Requires a function context with a binding data to retrieve the message correlation");

            if (TryGetUserPropertiesJson(context, out IDictionary<string, object> userPropertiesJson))
            {
                (string transactionIdW3C, string operationParentIdW3C) = userPropertiesJson.GetTraceParent();
                if (!string.IsNullOrWhiteSpace(transactionIdW3C))
                {
                    var clientForExistingParent = context.InstanceServices.GetRequiredService<TelemetryClient>();
                    return MessageCorrelationResult.Create(clientForExistingParent, transactionIdW3C, operationParentIdW3C);
                }
                
                if (userPropertiesJson.TryGetValue("Transaction-Id", out object transactionIdHierarchical))
                {
                    userPropertiesJson.TryGetValue("Operation-Parent-Id", out object operationParentIdHierarchical);

                    string operationId = Guid.NewGuid().ToString();
                    var correlationInfo = new MessageCorrelationInfo(operationId, transactionIdHierarchical.ToString(), operationParentIdHierarchical?.ToString());
                    return MessageCorrelationResult.Create(correlationInfo);
                }
            }

            string transactionId = Guid.NewGuid().ToString();
            var clientForNewParent = context.InstanceServices.GetRequiredService<TelemetryClient>();

            return MessageCorrelationResult.Create(clientForNewParent, transactionId, operationParentId: null);
        }

        private static bool TryGetUserPropertiesJson(FunctionContext context, out IDictionary<string, object> result)
        {
            if (context.BindingContext.BindingData.TryGetValue("UserProperties", out object userPropertiesObject)
                && userPropertiesObject != null)
            {
                string userPropertiesJson = userPropertiesObject.ToString();

                result = JsonSerializer.Deserialize<Dictionary<string, object>>(userPropertiesJson);
                return result != null;
            }

            result = null;
            return false;
        }
    }
#endif
}
