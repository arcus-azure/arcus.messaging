using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.MessagePump.Fixture
{
    public class TestMessagePump : Pumps.Abstractions.MessagePump
    {
        public TestMessagePump(
            string jobId,
            IConfiguration _, 
            IServiceProvider serviceProvider, 
            ILogger logger) 
            : base(serviceProvider, logger)
        {
            JobId = jobId;
            IsRunning = false;
        }

        public bool IsRunning { get; private set; }

        public override async Task StartProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Start test message pump");
            await base.StartAsync(cancellationToken);
            IsRunning = true;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Execute test message pump");
            return Task.CompletedTask;
        }

        public override async Task StopProcessingMessagesAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stop test message pump");
            await base.StopAsync(cancellationToken);
            IsRunning = false;
        }
    }
}
