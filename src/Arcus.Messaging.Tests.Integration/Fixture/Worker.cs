using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Hosting;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
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
        public static async Task<Worker> StartNewAsync(WorkerOptions options, [CallerMemberName] string memberName = null)
        {
            Guard.NotNull(options, nameof(options), "Requires a options instance that influence the test worker implementation");
            
            Console.WriteLine("Start '{0}' integration test", memberName);
            
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();
            options.ApplyOptions(hostBuilder);
            IHost host = hostBuilder.Build();
            
            var worker = new Worker(host, memberName);
            await worker._host.StartAsync();
            return worker;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Stop '{0}' integration test", _testName);

            try
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
        }
    }
}
