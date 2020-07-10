using System;
using System.Collections.Generic;
using System.IO;
using Arcus.Messaging.Tests.Integration.MessagePump;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a test configuration model with application key/value properties specific to this integration test suite.
    /// </summary>
    public class TestConfig : IConfigurationRoot
    {
        private readonly IConfigurationRoot _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestConfig"/> class.
        /// </summary>
        private TestConfig(IConfigurationRoot config)
        {
            Guard.NotNull(config, nameof(config));
            _config = config;
        }

        /// <summary>
        /// Creates the test configuration model for this integration test suite.
        /// </summary>
        /// <returns></returns>
        public static TestConfig Create()
        {
            var config =
                new ConfigurationBuilder()
                    .AddJsonFile(path: "appsettings.json")
                    .AddJsonFile(path: "appsettings.local.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

            return new TestConfig(config);
        }

        /// <summary>
        /// Gets the EventGrid topic URI for the test infrastructure.
        /// </summary>
        public string GetTestInfraEventGridTopicUri()
        {
            var value = _config.GetValue<string>("Arcus:Infra:EventGrid:TopicUri");
            Guard.NotNullOrWhitespace(value, "No non-blank EventGrid topic URI was found for the test infrastructure in the application configuration");

            return value;
        }

        /// <summary>
        /// Gets the EventGrid authentication key for the test infrastructure.
        /// </summary>
        public string GetTestInfraEventGridAuthKey()
        {
            var value = _config.GetValue<string>("Arcus:Infra:EventGrid:AuthKey");
            Guard.NotNullOrWhitespace(value, "No non-blank EventGrid authentication key was found for the test infrastructure in the application configuration");

            return value;
        }

        /// <summary>
        /// Gets the Service Bus connection string based on the given <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The type of the Service Bus entity.</param>
        public string GetServiceBusConnectionString(ServiceBusEntity entity)
        {
            switch (entity)
            {
                case ServiceBusEntity.Queue: return _config["Arcus:ServiceBus:ConnectionStringWithQueue"];
                case ServiceBusEntity.Topic: return _config["Arcus:ServiceBus:ConnectionStringWithTopic"];
                default:
                    throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown Service Bus entity");
            }
        }

        /// <summary>
        /// Gets the project directory where the fixtures are located.
        /// </summary>
        public DirectoryInfo GetIntegrationTestProjectDirectory()
        {
            return PathCombineWithSourcesDirectory(typeof(TestConfig).Assembly.GetName().Name);
        }

        /// <summary>
        /// Gets the project directory where the empty Service Bus worker project is located.
        /// </summary>
        public DirectoryInfo GetEmptyServiceBusWorkerProjectDirectory()
        {
            return PathCombineWithSourcesDirectory("Arcus.Messaging.Tests.Workers.ServiceBus");
        }

        private DirectoryInfo PathCombineWithSourcesDirectory(string subPath)
        {
            DirectoryInfo sourcesDirectory = GetBuildSourcesDirectory();

            string path = Path.Combine(sourcesDirectory.FullName, "src", subPath);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(
                    $"Cannot find sub-directory in build sources directory at: {path}");
            }

            return new DirectoryInfo(path);
        }

        private DirectoryInfo GetBuildSourcesDirectory()
        {
            const string buildSourcesDirectory = "Build.SourcesDirectory";

            string sourcesDirectory = _config.GetValue<string>(buildSourcesDirectory);
            Guard.NotNull(sourcesDirectory, nameof(sourcesDirectory), $"No build sources directory configured with the key: {buildSourcesDirectory}");
            Guard.For<ArgumentException>(
                () => !Directory.Exists(sourcesDirectory),
                $"No directory exists at {Path.GetFullPath(sourcesDirectory)}");

            return new DirectoryInfo(sourcesDirectory);
        }

        public KeyRotationConfig GetKeyRotationConfig()
        {
            var azureEnv = new ServiceBusQueue(
                subscriptionId: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:SubscriptionId"),
                tenantId: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:TenantId"),
                resourceGroup: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:ResourceGroupName"),
                @namespace: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:Namespace"),
                queueName: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:QueueName"),
                authorizationRuleName: _config.GetValue<string>("Arcus:KeyRotation:ServiceBus:AuthorizationRuleName"));

            var servicePrincipal = new ServicePrincipal(
                clientId: _config.GetValue<string>("Arcus:KeyRotation:ServicePrincipal:ClientId"),
                clientSecret: _config.GetValue<string>("Arcus:KeyRotation:ServicePrincipal:ClientSecret"));

            var secret = new KeyVaultSecret(
                vaultUri: _config.GetValue<string>("Arcus:KeyRotation:KeyVault:VaultUri"),
                secretName: _config.GetValue<string>("Arcus:KeyRotation:KeyVault:ConnectionStringSecretName"));

            return new KeyRotationConfig(secret, servicePrincipal, azureEnv);
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" />.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _config.GetSection(key);
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _config.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" /> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" />.</returns>
        public IChangeToken GetReloadToken()
        {
            return _config.GetReloadToken();
        }

        /// <summary>Gets or sets a configuration value.</summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get => _config[key];
            set => _config[key] = value;
        }

        /// <summary>
        /// Force the configuration values to be reloaded from the underlying <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s.
        /// </summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>
        /// The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _config.Providers;
    }
}
