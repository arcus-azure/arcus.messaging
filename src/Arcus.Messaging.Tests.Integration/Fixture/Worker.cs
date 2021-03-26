using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class WorkerOptions
    {
        public IServiceCollection Services { get; } = new ServiceCollection();
    }
    
    public class Worker : IAsyncDisposable
    {
        private IHost _host;

        private Worker(IHost host)
        {
            _host = host;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Worker StartNew(WorkerOptions options)
        {
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
            worker._host.StartAsync();

            return worker;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            Guard.NotNull(_host, nameof(_host), "Requires a host instance to dispose the worker");
            
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
