using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    }
    
    /// <summary>
    /// Represents a test worker implementation of a .NET SDK worker project, which includes all the options available in a worker project.
    /// Used to test message pumps and other messaging-related components that requires the hosted services setup.
    /// </summary>
    public class Worker : IAsyncDisposable
    {
        private readonly IHost _host;

        private Worker(IHost host)
        {
            _host = host;
        }
        
        /// <summary>
        /// Spawns a new test worker configurable with <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The configurable options to influence the content of the test worker.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public static Worker StartNew(WorkerOptions options)
        {
            Guard.NotNull(options, nameof(options), "Requires a options instance that influence the test worker implementation");
            
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    foreach (ServiceDescriptor service in options.Services)
                    {
                        services.Add(service);
                    }
                })
                .Build();
            
            var worker = new Worker(host);
            
            // Don't let the host block but continue while the host is starting.
            worker._host.StartAsync();
            return worker;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
