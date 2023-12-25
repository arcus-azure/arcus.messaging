using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Testing.Logging;
using Azure.Messaging.ServiceBus;
using Polly;
using Polly.Wrap;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.ServiceBus
{
    public class ServiceBusSenderExtensionsTests
    {
        private const string DependencyIdPattern = @"with ID [a-z0-9]{8}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{12}";

        private readonly TestConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusSenderExtensionsTests" /> class.
        /// </summary>
        public ServiceBusSenderExtensionsTests()
        {
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task SendMessage_WithMessageCorrelation_TracksMessage()
        {
            // Arrange
            Order order = OrderGenerator.Generate();
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            string connectionString = _config.GetServiceBusQueueConnectionString();
            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);

            await using (var client = new ServiceBusClient(connectionString))
            {
                await using (ServiceBusSender sender = client.CreateSender(connectionStringProperties.EntityPath))
                {
                    // Act
                    await sender.SendMessageAsync(order, correlation, logger);
                }

                // Assert
               await RetryAssertUntilServiceBusMessageIsAvailableAsync(client, connectionStringProperties.EntityPath, message =>
               {
                   var actual = message.Body.ToObjectFromJson<Order>();
                   Assert.Equal(order.Id, actual.Id);

                   Assert.Equal(message.ApplicationProperties[PropertyNames.TransactionId], correlation.TransactionId);
                   Assert.False(string.IsNullOrWhiteSpace(message.ApplicationProperties[PropertyNames.OperationParentId].ToString()));

                   string logMessage = Assert.Single(logger.Messages);
                   Assert.Contains("Dependency", logMessage);
                   Assert.Matches(DependencyIdPattern, logMessage);
               });
            }
        }

        [Fact]
        public async Task SendMessage_WithCustomOptions_TracksMessage()
        {
            // Arrange
            Order order = OrderGenerator.Generate();
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var dependencyId = $"parent-{Guid.NewGuid()}";
            string transactionIdPropertyName = "My-Transaction-Id", upstreamServicePropertyName = "My-UpstreamService-Id";
            string key = $"key-{Guid.NewGuid()}", value = $"value-{Guid.NewGuid()}";
            var telemetryContext = new Dictionary<string, object> { [key] = value };
            var logger = new InMemoryLogger();

            string connectionString = _config.GetServiceBusQueueConnectionString();
            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);

            await using (var client = new ServiceBusClient(connectionString))
            {
                await using (ServiceBusSender sender = client.CreateSender(connectionStringProperties.EntityPath))
                {
                    // Act
                    await sender.SendMessageAsync(order, correlation, logger, options =>
                    {
                        options.TransactionIdPropertyName = transactionIdPropertyName;
                        options.UpstreamServicePropertyName = upstreamServicePropertyName;
                        options.GenerateDependencyId = () => dependencyId;
                        options.AddTelemetryContext(telemetryContext);
                    });
                }

                // Assert
                string logMessage = Assert.Single(logger.Messages);
                Assert.Contains("Dependency", logMessage);
                Assert.Matches($"with ID {dependencyId}", logMessage);
                Assert.Contains(key, logMessage);
                Assert.Contains(value, logMessage);
                await RetryAssertUntilServiceBusMessageIsAvailableAsync(client, connectionStringProperties.EntityPath, message =>
                {
                    var actual = message.Body.ToObjectFromJson<Order>();
                    Assert.Equal(order.Id, actual.Id);

                    Assert.Equal(correlation.TransactionId, message.ApplicationProperties[transactionIdPropertyName]);
                    Assert.Equal(dependencyId, message.ApplicationProperties[upstreamServicePropertyName]);
                });
            }
        }

        private static MessageCorrelationInfo GenerateMessageCorrelationInfo()
        {
            return new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
        }

        private static async Task RetryAssertUntilServiceBusMessageIsAvailableAsync(ServiceBusClient client, string entityPath, Action<ServiceBusReceivedMessage> assertion)
        {
            AsyncPolicyWrap policy =
                Policy.TimeoutAsync(TimeSpan.FromSeconds(30))
                      .WrapAsync(Policy.Handle<Exception>()
                                       .WaitAndRetryForeverAsync(index => TimeSpan.FromMilliseconds(500)));

            await using (ServiceBusReceiver receiver = client.CreateReceiver(entityPath))
            {
                await policy.ExecuteAsync(async () =>
                {
                    IAsyncEnumerable<ServiceBusReceivedMessage> messages = receiver.ReceiveMessagesAsync();
                    var exceptions = new Collection<Exception>();

                    await foreach (ServiceBusReceivedMessage message in messages)
                    {
                        try
                        {
                            assertion(message);
                            await receiver.CompleteMessageAsync(message);
                            
                            return;
                        }
                        catch (Exception exception)
                        {
                            exceptions.Add(exception);
                        }
                    }

                    throw exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions);
                });
            }
        }
    }
}
