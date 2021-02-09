using System;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Tests.Workers.ServiceBus
{
    public class ServiceBusQueueTrackCorrelationOnExceptionProgram
    {
        public static void main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                CreateHostBuilder(args)
                        .Build()
                        .Run();
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddCommandLine(args);
                    configuration.AddEnvironmentVariables();
                })
                .UseSerilog(UpdateLoggerConfiguration)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddServiceBusQueueMessagePump(configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"], options => options.AutoComplete = true)
                            .WithServiceBusMessageHandler<OrdersSabotageAzureServiceBusMessageHandler, Order>();

                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT", builder => builder.AddCheck("sample", () => HealthCheckResult.Healthy()));
                });

        private static void UpdateLoggerConfiguration(
            HostBuilderContext hostContext,
            LoggerConfiguration currentLoggerConfiguration)
        {
            var instrumentationKey = hostContext.Configuration.GetValue<string>("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY");

            currentLoggerConfiguration
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithVersion()
                .Enrich.WithComponentName("Service Bus Queue Worker")
                .WriteTo.Console()
                .WriteTo.AzureApplicationInsights(instrumentationKey);
        }
    }
}