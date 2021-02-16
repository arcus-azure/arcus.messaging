using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Representation of a project containing a Azure Service Bus message pump.
    /// </summary>
    public class WorkerProject : IDisposable
    {
        public const int HealthPort = 40643;
        
        private readonly Process _process;
        private readonly DirectoryInfo _projectDirectory;
        private readonly ILogger _logger;
        
        private bool _disposed;

        private WorkerProject(DirectoryInfo projectDirectory, ILogger logger)
        {
            _process = new Process();
            _projectDirectory = projectDirectory;
            _logger = logger;
        }
        
        /// <summary>
        /// Starts a new project with a Azure Service Bus message pump from a given <typeparamref name="TProgram"/>.
        /// </summary>
        /// <typeparam name="TProgram">The type of the 'Program.cs' that the project should have.</typeparam>
        /// <param name="config">The set of key/value pairs to extract the required configuration values for the project.</param>
        /// <param name="logger">The logger to write diagnostic messages during the creation of the project.</param>
        /// <param name="commandArguments">The additional CLI arguments to send to the project at startup.</param>
        public static async Task<WorkerProject> StartNewWithAsync<TProgram>(
            TestConfig config, 
            ILogger logger,
            params CommandArgument[] commandArguments)
        {
            Type programType = typeof(TProgram);
            WorkerProject project = await StartNewWithAsync(programType, config, logger, startupTcpVerification: true, commandArguments);

            return project;
        }

        /// <summary>
        /// Starts a new project with a Azure Service Bus message pump from a given <typeparamref name="TProgram"/>.
        /// </summary>
        /// <typeparam name="TProgram">The type of the 'Program.cs' that the project should have.</typeparam>
        /// <param name="config">The set of key/value pairs to extract the required configuration values for the project.</param>
        /// <param name="logger">The logger to write diagnostic messages during the creation of the project.</param>
        /// <param name="startupTcpVerification">The flag indicating whether or not the project should be verified if it's started up correctly.</param>
        /// <param name="commandArguments">The additional CLI arguments to send to the project at startup.</param>
        public static async Task<WorkerProject> StartNewWithAsync<TProgram>(
            TestConfig config, 
            ILogger logger,
            bool startupTcpVerification = true,
            params CommandArgument[] commandArguments)
        {
            Type programType = typeof(TProgram);
            WorkerProject project = await StartNewWithAsync(programType, config, logger, startupTcpVerification, commandArguments);

            return project;
        }

        /// <summary>
        /// Starts a new project with a Azure Service Bus message pump from a given <paramref name="programType"/>.
        /// </summary>
        /// <param name="programType">The type of the 'Program.cs' that the project should have.</param>
        /// <param name="config">The set of key/value pairs to extract the required configuration values for the project.</param>
        /// <param name="logger">The logger to write diagnostic messages during the creation of the project.</param>
        /// <param name="commandArguments">The additional CLI arguments to send to the project at startup.</param>
        public static async Task<WorkerProject> StartNewWithAsync(
            Type programType,
            TestConfig config,
            ILogger logger,
            params CommandArgument[] commandArguments)
        {
            WorkerProject project = await StartNewWithAsync(programType, config, logger, startupTcpVerification: true, commandArguments);
            return project;
        }

        /// <summary>
        /// Starts a new project with a Azure Service Bus message pump from a given <paramref name="programType"/>.
        /// </summary>
        /// <param name="programType">The type of the 'Program.cs' that the project should have.</param>
        /// <param name="config">The set of key/value pairs to extract the required configuration values for the project.</param>
        /// <param name="logger">The logger to write diagnostic messages during the creation of the project.</param>
        /// <param name="startupTcpVerification">The flag indicating whether or not the project should be verified if it's started up correctly.</param>
        /// <param name="commandArguments">The additional CLI arguments to send to the project at startup.</param>
        public static async Task<WorkerProject> StartNewWithAsync(
            Type programType,
            TestConfig config,
            ILogger logger,
            bool startupTcpVerification = true,
            params CommandArgument[] commandArguments)
        {
            Guard.NotNull(programType, nameof(programType));
            Guard.For<ArgumentException>(
                () => !programType.Name.EndsWith("Program"), 
                "Requires a type that is considered a startup type with a type name ending with '...Program'");

            DirectoryInfo integrationTestDirectory = config.GetIntegrationTestProjectDirectory();
            DirectoryInfo emptyServiceBusWorkerDirectory = config.GetEmptyServiceBusWorkerProjectDirectory();

            ReplaceProgramFile(programType, integrationTestDirectory, emptyServiceBusWorkerDirectory);

            var project = new WorkerProject(emptyServiceBusWorkerDirectory, logger);

            project.Start(commandArguments.Prepend(CommandArgument.CreateOpen("ARCUS_HEALTH_PORT", HealthPort)));
            if (startupTcpVerification)
            {
                await project.WaitUntilWorkerIsAvailableAsync(HealthPort);
            }

            return project;
        }

        private static void ReplaceProgramFile(
            Type typeOfProgram,
            DirectoryInfo integrationTestDirectory,
            DirectoryInfo emptyServiceBusWorkerDirectory)
        {
            FileInfo[] programFiles = integrationTestDirectory.GetFiles(typeOfProgram.Name + ".cs", SearchOption.AllDirectories);
            if (programFiles.Length <= 0)
            {
                throw new FileNotFoundException(
                    $"Cannot find program file with the name '{typeOfProgram.Name}.cs' in the integration test project directory. "
                    + "Please provide such a file so it can be set as the program file of the empty Service Bus worker project");
            }

            if (programFiles.Length > 1)
            {
                throw new FileNotFoundException(
                    $"Cannot use the '{typeOfProgram.Name}.cs' program file because more than one such a file exists in the integration test project directory. "
                    + "Please rename one of the types so it can be distinguished from each other.");
            }

            FileInfo programFile = programFiles[0];
            string contents = File.ReadAllText(programFile.FullName);
            contents = 
                contents.Replace(typeOfProgram.Name, "Program")
                        .Replace("main", "Main");

            string destinationPath = Path.Combine(emptyServiceBusWorkerDirectory.FullName, "Program.cs");

            File.WriteAllText(destinationPath, contents);
        }

        private void Start(IEnumerable<CommandArgument> commandArguments)
        {
            RunDotNet($"build --nologo --no-incremental -c Release {_projectDirectory.FullName}");
            
            string targetAssembly = Path.Combine(_projectDirectory.FullName, $"bin/Release/netcoreapp3.1/Arcus.Messaging.Tests.Workers.ServiceBus.dll");
            string exposedSecretsCommands = String.Join(" ", commandArguments.Select(arg => arg.ToExposedString()));
            string runCommand = $"exec {targetAssembly} {exposedSecretsCommands}";

            string hiddenSecretsCommands = String.Join(" ", commandArguments.Select(arg => arg.ToString()));
            _logger.LogInformation("> dotnet exec {Assembly} {Commands}", targetAssembly, hiddenSecretsCommands);

            var processInfo = new ProcessStartInfo("dotnet", runCommand)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _projectDirectory.FullName,
            };

            _process.StartInfo = processInfo;
            _process.Start();
        }

        private async Task WaitUntilWorkerIsAvailableAsync(int healthPort)
        {
            IAsyncPolicy waitAndRetryForeverAsync =
                Policy.Handle<Exception>()
                      .WaitAndRetryForeverAsync(retryNumber => TimeSpan.FromSeconds(1));

            PolicyResult result = 
                await Policy.TimeoutAsync(TimeSpan.FromSeconds(15))
                            .WrapAsync(waitAndRetryForeverAsync)
                            .ExecuteAndCaptureAsync(() => TryToConnectToTcpListenerAsync(healthPort));

            if (result.Outcome == OutcomeType.Successful)
            {
                _logger.LogInformation("Test Service Bus worker project fully started at: localhost:{Port}", healthPort);
            }
            else
            {
                _logger.LogError("Test Service Bus project could not be started");
                throw new CommunicationException(
                    "The test project created from the Service Bus project template doesn't seem to be running, "
                    + "please check any build or runtime errors that could occur when the test project was created");
            }
        }

        private static async Task TryToConnectToTcpListenerAsync(int tcpPort)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), tcpPort);
                }
                finally
                {
                    client.Close();
                }
            }
        }

        private void RunDotNet(string command)
        {
            try
            {
                _logger.LogInformation("> dotnet {Command}", command);
            }
            catch
            {
                Console.WriteLine("> dotnet {0}", command);
            }

            var startInfo = new ProcessStartInfo("dotnet", command)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            PolicyResult[] results =
            {
                RetryAction(StopProject)
            };

            IEnumerable<Exception> exceptions =
                results.Where(result => result.Outcome == OutcomeType.Failure)
                       .Select(result => result.FinalException);

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        private static PolicyResult RetryAction(Action action)
        {
            return Policy.Timeout(TimeSpan.FromSeconds(30))
                         .Wrap(Policy.Handle<Exception>()
                                     .WaitAndRetryForever(_ => TimeSpan.FromSeconds(1)))
                         .ExecuteAndCapture(action);
        }

        private void StopProject()
        {
            _logger.LogTrace("Stopping Service Bus worker project...");
            if (!_process.HasExited)
            {
                _logger.LogTrace("Killing Service Bus worker project...");
                _process.Kill(entireProcessTree: true);
                _logger.LogInformation("Killed Service Bus worker project!");
            }

            _process.Dispose();
            _logger.LogInformation("Service Bus worker project stopped!");
        }
    }
}
