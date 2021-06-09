using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue
{
    public class ServiceBusFunction
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly string _jobId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusFunction" /> class.
        /// </summary>
        public ServiceBusFunction(IAzureServiceBusMessageRouter messageRouter)
        {
            _messageRouter = messageRouter;
            _jobId = Guid.NewGuid().ToString();
        }

        [FunctionName("order")]
        public async Task Run(
            [ServiceBusTrigger("docker-az-func-queue", Connection = "SERVICEBUS_CONNECTIONSTRING")] ServiceBusReceivedMessage message,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {message.MessageId}");
            
            var context = new AzureServiceBusMessageContext(message.MessageId, _jobId, AzureServiceBusSystemProperties.CreateFrom(message), message.ApplicationProperties);
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();
            await _messageRouter.RouteMessageAsync(message, context, correlationInfo, cancellationToken);
        }
    }
}
