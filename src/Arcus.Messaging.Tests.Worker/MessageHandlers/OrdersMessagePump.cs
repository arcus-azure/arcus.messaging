using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Contracts.v1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Worker.MessageHandlers
{
    public class OrdersMessagePump : AzureServiceBusMessagePump<Order>
    {
        public OrdersMessagePump(IConfiguration configuration, ILogger<OrdersMessagePump> logger)
            : base(configuration, logger)
        {
        }

        protected override async Task ProcessMessageAsync(Order orderMessage, AzureServiceBusMessageContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
        {
            Logger.LogInformation(
                "Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}",
                orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, orderMessage.Customer.FirstName, orderMessage.Customer.LastName);

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            Logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
        }
    }
}