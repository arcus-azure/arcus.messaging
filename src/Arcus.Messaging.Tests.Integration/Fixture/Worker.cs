using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents the configurable options to influence the test <see cref="Worker"/>.
    /// </summary>
    public class WorkerOptions
    {
        /// <summary>
        /// Gets the services that will be included in the test <see cref="Worker"/>.
        /// </summary>
        public IServiceCollection Services { get; } = new ServiceCollection();

        /// <summary>
        /// Gets the configuration instance that will be included in the test <see cref="Worker"/> and which will result in an <see cref="IConfiguration"/> instance.
        /// </summary>
        public IDictionary<string, string> Configuration { get; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Represents a test worker implementation of a .NET SDK worker project, which includes all the options available in a worker project.
    /// Used to test message pumps and other messaging-related components that requires the hosted services setup.
    /// </summary>
    public class Worker : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly string _testName;
        
        private Worker(IHost host, string testName)
        {
            _host = host;
            _testName = testName;
        }

        /// <summary>
        /// Spawns a new test worker configurable with <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The configurable options to influence the content of the test worker.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public static Worker StartNew(WorkerOptions options, [CallerMemberName] string memberName = null)
        {
            Guard.NotNull(options, nameof(options), "Requires a options instance that influence the test worker implementation");
            
            Console.WriteLine("Start '{0}' integration test", memberName);
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureAppConfiguration(config => config.AddInMemoryCollection(options.Configuration))
                .ConfigureServices(services =>
                {
                    foreach (ServiceDescriptor service in options.Services)
                    {
                        services.Add(service);
                    }
                })
                .Build();
            
            var worker = new Worker(host, memberName);
            
            // Don't let the host block but continue while the host is starting.
            Task.Run(() => worker._host.StartAsync());
            return worker;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Stop '{0}' integration test", _testName);
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
