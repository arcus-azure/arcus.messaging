using Arcus.Testing;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class StorageAccountConfig
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAccountConfig" /> class.
        /// </summary>
        public StorageAccountConfig(string name, string key)
        {
            Name = name;
            ConnectionString = $"DefaultEndpointsProtocol=https;AccountName={name};AccountKey={key};EndpointSuffix=core.windows.net";
        }

        public string Name { get; }
        public string ConnectionString { get; }
    }

    public static class StorageAccountConfigExtensions
    {
        public static StorageAccountConfig GetStorageAccount(this TestConfig config)
        {
            return new StorageAccountConfig(
                config["Arcus:StorageAccount:Name"],
                config["Arcus:StorageAccount:Key"]);
        }
    }
}