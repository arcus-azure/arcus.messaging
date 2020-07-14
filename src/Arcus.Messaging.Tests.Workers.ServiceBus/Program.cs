using System.Collections.Generic;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Security.Core;
using Arcus.Security.Providers.AzureKeyVault;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Tests.Workers.ServiceBus
{
    public class Program
    {
        public static void Main(string[] args)
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
                    configuration.AddInMemoryCollection(new []{ new KeyValuePair<string, string>("ARCUS_HEALTH_PORT", "5000") });
                })
                .ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole(options => options.IncludeScopes = true))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ISecretProvider>(serviceProvider =>
                    {
                        var authentication = new ServicePrincipalAuthentication(
                            "d1c9a6b8-e26e-4bd7-85a7-98140ce73cb1",
                            "B7y/GrQV6T.74UZpNBxGose=fIBEqa=W");

                        return new KeyVaultSecretProvider(
                            //new ServicePrincipalAuthentication(
                            //    "88e84e7b-7f06-45e3-933f-4945270e0f60",
                            //    "23.HBxx@Oft9e3XjWRczJCBDXTazz/c/"),
                            authentication,
                            new KeyVaultConfiguration(
                                /*"https://arcustesting.vault.azure.net/"*/
                                "https://arcus-messaging-dev-we.vault.azure.net/"));
                    });
                    services.AddTransient(svc =>
                    {
                        var configuration = svc.GetRequiredService<IConfiguration>();
                        var eventGridTopic =
                            "https://arcus-event-grid-dev-we-integration-tests-cncf-ce.westeurope-1.eventgrid.azure.net/api/events";
                        var eventGridKey = "M1C7nia9SXWroDFyzl0dOMRL+1G2cI9D1+PCXkMsrd8=";

                        return EventGridPublisherBuilder
                            .ForTopic(eventGridTopic)
                            .UsingAuthenticationKey(eventGridKey)
                            .Build();
                    });
                    services.AddServiceBusQueueMessagePump("arcus-messaging-keyrotate-servicebus-connectionstring")
                            .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT", builder => builder.AddCheck("sample", () => HealthCheckResult.Healthy()));
                });
    }
}