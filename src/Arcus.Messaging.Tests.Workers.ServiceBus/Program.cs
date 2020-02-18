using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                })
                .ConfigureServices((hostContext, services) =>
                {
                    
                });
    }
}
