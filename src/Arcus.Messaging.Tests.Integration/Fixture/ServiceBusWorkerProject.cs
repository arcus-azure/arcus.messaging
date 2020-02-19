using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// 
    /// </summary>
    public class ServiceBusWorkerProject : IDisposable
    {
        private readonly Process _process;
        private readonly DirectoryInfo _projectDirectory;
        private readonly ITestOutputHelper _logger;
        
        private bool _disposed;

        private ServiceBusWorkerProject(DirectoryInfo projectDirectory, ITestOutputHelper outputWriter)
        {
            _process = new Process();
            _projectDirectory = projectDirectory;
            _logger = outputWriter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TProgram"></typeparam>
        /// <param name="config"></param>
        /// <param name="outputWriter"></param>
        /// <param name="commandArguments"></param>
        /// <returns></returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithAsync<TProgram>(
            TestConfig config, 
            ITestOutputHelper outputWriter,
            params CommandArgument[] commandArguments)
        {
            Type typeOfProgram = typeof(TProgram);
            Guard.NotNull(typeOfProgram, nameof(typeOfProgram));
            Guard.For<ArgumentException>(
                () => !typeOfProgram.Name.EndsWith("Program"), 
                "Requires a type that is considered a startup type with a type name ending with '...Program'");

            DirectoryInfo integrationTestDirectory = config.GetIntegrationTestProjectDirectory();
            DirectoryInfo emptyServiceBusWorkerDirectory = config.GetEmptyServiceBusWorkerProjectDirectory();

            ReplaceProgramFile(typeOfProgram, integrationTestDirectory, emptyServiceBusWorkerDirectory);

            var project = new ServiceBusWorkerProject(emptyServiceBusWorkerDirectory, outputWriter);

            const int healthPort = 40643;
            project.Start(commandArguments.Prepend(CommandArgument.CreateOpen("ARCUS_HEALTH_PORT", healthPort)));
            await project.WaitUntilWorkerIsAvailableAsync(healthPort);

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
            RunDotNet($"build -c Release {_projectDirectory.FullName}");
            
            string targetAssembly = Path.Combine(_projectDirectory.FullName, $"bin/Release/netcoreapp3.0/Arcus.Messaging.Tests.Workers.ServiceBus.dll");
            string exposedSecretsCommands = String.Join(" ", commandArguments.Select(arg => arg.ToExposedString()));
            string runCommand = $"exec {targetAssembly} {exposedSecretsCommands}";

            string hiddenSecretsCommands = String.Join(" ", commandArguments.Select(arg => arg.ToString()));
            _logger.WriteLine("> dotnet {0}", $"exec {targetAssembly} {hiddenSecretsCommands}");

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
                await Policy.TimeoutAsync(TimeSpan.FromSeconds(10))
                            .WrapAsync(waitAndRetryForeverAsync)
                            .ExecuteAndCaptureAsync(() => TryToConnectToTcpListenerAsync(healthPort));

            if (result.Outcome == OutcomeType.Successful)
            {
                _logger.WriteLine("Test Service Bus worker project fully started at: localhost:{0}", healthPort);
            }
            else
            {
                _logger.WriteLine("Test Service Bus project could not be started");
                throw new CommunicationException(
                    "The test project created from the Service Bus project template doesn't seem to be running, "
                    + "please check any build or runtime errors that could occur when the test project was created");
            }
        }

        private static async Task TryToConnectToTcpListenerAsync(int tcpPort)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), tcpPort);
                client.Close();
            }
        }


        private void RunDotNet(string command)
        {
            try
            {
                _logger.WriteLine("> dotnet {0}", command);
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

            using Process process = Process.Start(startInfo);
            process.WaitForExit();

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
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
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            _process.Dispose();
        }
    }
}
