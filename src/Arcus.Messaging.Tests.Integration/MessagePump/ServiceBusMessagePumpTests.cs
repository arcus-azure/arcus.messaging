using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Workers.ServiceBus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Trait("Category", "Integration")]
    public class ServiceBusMessagePumpTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task ServiceBusMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            const ServiceBusEntity entity = ServiceBusEntity.Queue;

            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", config.GetServiceBusConnectionString(entity)),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueProgram>(config, _outputWriter, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(entity, config, _outputWriter))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync();
                }
            }
        }
    }
}
