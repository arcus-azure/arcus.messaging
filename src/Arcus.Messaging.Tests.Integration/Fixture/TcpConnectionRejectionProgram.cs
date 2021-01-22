using System;
using System.Collections.Generic;
using System.Text;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Tests.Workers.ServiceBus
{
    public class TcpConnectionRejectionProgram
    {
        public static void main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddCommandLine(args);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole(options => options.IncludeScopes = true))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTcpHealthProbes(
                        "ARCUS_HEALTH_PORT", 
                        builder => builder.AddCheck("toggle", 
                            () => Environment.GetEnvironmentVariable("ARCUS_HEALTH_STATUS", EnvironmentVariableTarget.Machine) == "Healthy" 
                                ? HealthCheckResult.Healthy() 
                                : HealthCheckResult.Unhealthy()),
                        options => options.RejectTcpConnectionWhenUnhealthy = true,
                        options =>
                        {
                            options.Delay = TimeSpan.Zero;
                            options.Period = TimeSpan.FromSeconds(1);
                        });
                });
    }
}