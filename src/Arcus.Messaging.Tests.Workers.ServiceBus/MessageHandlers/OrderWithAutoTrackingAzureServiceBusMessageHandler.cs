using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers
{
    public class OrderWithAutoTrackingAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly bool _isSuccessful;
        private readonly ILogger<OrderWithAutoTrackingAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderWithAutoTrackingAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public OrderWithAutoTrackingAzureServiceBusMessageHandler(ILogger<OrderWithAutoTrackingAzureServiceBusMessageHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderWithAutoTrackingAzureServiceBusMessageHandler"/> class.
        /// </summary>
        public OrderWithAutoTrackingAzureServiceBusMessageHandler(bool isSuccessful, ILogger<OrderWithAutoTrackingAzureServiceBusMessageHandler> logger)
            : this(logger)
        {
            _isSuccessful = isSuccessful;
        }

        public Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogAzureKeyVaultDependency("https://my-vault.azure.net", "Sql-connection-string", isSuccessful: true, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            SimulateSqlQueryWithMicrosoftTracking();

            if (!_isSuccessful)
            {
                throw new InvalidOperationException(
                    "[Test] Sabotage this message processing to let the message correlation system pick up an 'unsuccessful request'");
            }

            return Task.CompletedTask;
        }

        private static void SimulateSqlQueryWithMicrosoftTracking()
        {
            try
            {
                using (var connection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Database=master"))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Orders";
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch
            {
                // Ignore:
                // We only want to simulate a SQL connection/command, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }
    }
}
