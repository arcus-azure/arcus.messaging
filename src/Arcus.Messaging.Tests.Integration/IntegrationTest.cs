using Arcus.Messaging.Tests.Integration.Logging;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration
{
    /// <summary>
    /// Represents default environment values required in describing integration tests.
    /// </summary>
    public class IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest"/> class.
        /// </summary>
        public IntegrationTest(ITestOutputHelper testOutput)
        {
            Logger = new XunitTestLogger(testOutput);

            Configuration = 
                new ConfigurationBuilder()
                    .AddJsonFile(path: "appsettings.json")
                    .AddJsonFile(path: "appsettings.local.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
        }

        /// <summary>
        /// Gets the configuration of the current environment.
        /// </summary>
        protected IConfiguration Configuration { get; }
        
        /// <summary>
        /// Gets the logger for this test run.
        /// </summary>
        protected XunitTestLogger Logger { get; }
    }
}
