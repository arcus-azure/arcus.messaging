using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Observability.Telemetry.Core;
using Arcus.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public class MessageRouterTests
    {
        [Fact]
        public async Task WithMessageRouting_FailureDuringRouteWithFallback_TracksCorrelation()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            var options = new WorkerOptions();
            options.Configure(host => host.UseSerilog((context, config) =>
            {
                config.Enrich.FromLogContext()
                      .WriteTo.Sink(spySink);
            }));

            options.AddMessageRouting()
                   .WithMessageHandler<SabotageTestMessageHandler<Order, MessageContext>, Order>();

            string operationId = $"operation-{Guid.NewGuid()}";
            string transactionId = $"transaction-{Guid.NewGuid()}";

            await using (var worker = await Worker.StartNewAsync(options))
            {
                var router = worker.Services.GetRequiredService<IMessageRouter>();

                Order order = OrderGenerator.Generate();
                var context = new MessageContext("message-id", new Dictionary<string, object>());
                var correlationInfo = new MessageCorrelationInfo(operationId, transactionId);
                string json = JsonConvert.SerializeObject(order);

                // Act / Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => router.RouteMessageAsync(json, context, correlationInfo, CancellationToken.None));
                
                Assert.Contains("cannot correctly process message", exception.Message);
                Assert.Contains(spySink.CurrentLogEmits,
                    log => log.Exception?.Message.Contains("Sabotage") is true 
                           && log.ContainsProperty(ContextProperties.Correlation.OperationId, operationId) 
                           && log.ContainsProperty(ContextProperties.Correlation.TransactionId, transactionId));
            }
        }
    }
}
