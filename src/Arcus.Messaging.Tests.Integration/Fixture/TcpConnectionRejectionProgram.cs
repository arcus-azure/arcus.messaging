using System;
using System.Threading.Tasks;
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
                .ConfigureLogging(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole(options => options.IncludeScopes = true))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTcpHealthProbes(
                        "ARCUS_HEALTH_PORT", 
                        builder => builder.AddAsyncCheck("toggle", async () =>
                        {
                            if (hostContext.Configuration.GetValue<string>("ARCUS_HEALTH_STATUS") == "Healthy")
                            {
                                return HealthCheckResult.Healthy();
                            }

                            // Adding some extra waiting time so we don't overload the test log with unhealthy status reports.
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            return HealthCheckResult.Unhealthy();
                        }),
                        options => options.RejectTcpConnectionWhenUnhealthy = true,
                        options =>
                        {
                            options.Delay = TimeSpan.Zero;
                        });
                });
    }
}