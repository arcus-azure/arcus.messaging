using Arcus.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Arcus.Messaging.Tests.Integration
{
    /// <summary>
    /// Represents default environment values required in describing integration tests.
    /// </summary>
    public abstract class IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest"/> class.
        /// </summary>
        protected IntegrationTest(ITestOutputHelper testOutput)
        {
            Logger = new XunitTestLogger(testOutput);
            Configuration = TestConfig.Create();
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
