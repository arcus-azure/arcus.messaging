using System;
using System.Collections.Generic;
using System.Text;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation.Extensions;
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
    public class ServiceBusQueueSecretNewVersionReAuthenticateProgram
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
                .ConfigureSecretStore((config, stores) =>
                {
                    stores.AddAzureKeyVaultWithServicePrincipal(
                            config["ARCUS_KEYVAULT_VAULTURI"], 
                            config["ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTID"], 
                            config["ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTSECRET"])
                          .AddConfiguration(config);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient(svc =>
                    {
                        var configuration = svc.GetRequiredService<IConfiguration>();
                        var eventGridTopic = configuration["EVENTGRID_TOPIC_URI"];
                        var eventGridKey = configuration["EVENTGRID_AUTH_KEY"];

                        return EventGridPublisherBuilder
                               .ForTopic(eventGridTopic)
                               .UsingAuthenticationKey(eventGridKey)
                               .Build();
                    });

                    string jobId = Guid.NewGuid().ToString();
                    services.AddServiceBusQueueMessagePump(hostContext.Configuration["ARCUS_KEYVAULT_CONNECTIONSTRINGSECRETNAME"], options =>
                            {
                                options.JobId = jobId;
                                
                                // Unrealistic big maximum exception count so that we're certain that the message pump gets restarted based on the notification and not the unauthorized exception.
                                options.MaximumUnauthorizedExceptionsBeforeRestart = 1000;
                            })
                            .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>()
                            .WithReAuthenticationOnNewSecretVersion(
                                jobId: jobId, 
                                subscriptionNamePrefix: "TestSub", 
                                serviceBusTopicConnectionStringSecretKey: "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING");

                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT", builder => builder.AddCheck("sample", () => HealthCheckResult.Healthy()));
                });
    }
}
