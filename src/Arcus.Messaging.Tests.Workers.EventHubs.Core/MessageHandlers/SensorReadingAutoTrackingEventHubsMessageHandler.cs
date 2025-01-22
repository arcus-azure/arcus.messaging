using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers
{
    public class SensorReadingAutoTrackingEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorReading>
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingAutoTrackingEventHubsMessageHandler" /> class.
        /// </summary>
        public SensorReadingAutoTrackingEventHubsMessageHandler(ILogger<SensorReadingAutoTrackingEventHubsMessageHandler> logger)
        {
            _logger = logger;
        }

        public Task ProcessMessageAsync(
            SensorReading message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Process Order '{OrderId}'", message.SensorId);
            _logger.LogAzureKeyVaultDependency("https://my-vault.azure.net", "Sql-connection-string", isSuccessful: true, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            SimulateSqlQueryWithMicrosoftTracking();

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
