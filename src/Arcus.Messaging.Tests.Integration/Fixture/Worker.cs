using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly ICollection<Action<IHostBuilder>> _additionalHostOptions = new Collection<Action<IHostBuilder>>();
        
        /// <summary>
        /// Gets the services that will be included in the test <see cref="Worker"/>.
        /// </summary>
        public IServiceCollection Services { get; } = new ServiceCollection();

        /// <summary>
        /// Gets the configuration instance that will be included in the test <see cref="Worker"/> and which will result in an <see cref="IConfiguration"/> instance.
        /// </summary>
        public IDictionary<string, string> Configuration { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Adds an additional configuration option on the to-be-created <see cref="IHostBuilder"/>.
        /// </summary>
        /// <param name="additionalHostOption">The action that configures the additional option.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="additionalHostOption"/> is <c>null</c>.</exception>
        public void Configure(Action<IHostBuilder> additionalHostOption)
        {
            Guard.NotNull(additionalHostOption, nameof(additionalHostOption), "Requires an custom action that will add the additional hosting option");
            _additionalHostOptions.Add(additionalHostOption);
        }

        /// <summary>
        /// Applies the previously configured additional host options to the given <paramref name="hostBuilder"/>.
        /// </summary>
        /// <param name="hostBuilder">The builder instance to apply the additional host options to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="hostBuilder"/> is <c>null</c>.</exception>
        internal void ApplyOptions(IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(Configuration))
                       .ConfigureServices(services =>
                       {
                           foreach (ServiceDescriptor service in Services)
                           {
                               services.Add(service);
                           }
                       });
            
            foreach (Action<IHostBuilder> additionalHostOption in _additionalHostOptions)
            {
                additionalHostOption(hostBuilder);
            }
        }
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
            IHostBuilder hostBuilder =
                Host.CreateDefaultBuilder()
                    .ConfigureLogging(logging => logging.ClearProviders());
            
            options.ApplyOptions(hostBuilder);
            IHost host = hostBuilder.Build();
            
            var worker = new Worker(host, memberName);
            
            // Don't let the host block but continue while the host is starting.
            worker._host.StartAsync().GetAwaiter().GetResult();
            return worker;
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
            IHostBuilder hostBuilder =
                Host.CreateDefaultBuilder()
                    .ConfigureLogging(logging => logging.ClearProviders());
            
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
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
