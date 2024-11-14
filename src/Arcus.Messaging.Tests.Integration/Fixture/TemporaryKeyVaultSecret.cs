using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class TemporaryKeyVaultSecret : IAsyncDisposable
    {
        private readonly string _secretName;
        private readonly SecretClient _client;
        private readonly ILogger _logger;

        private TemporaryKeyVaultSecret(string secretName, SecretClient client, ILogger logger)
        {
            _secretName = secretName;
            _client = client;
            _logger = logger;
        }

        public string Name => _secretName;

        public static async Task<TemporaryKeyVaultSecret> CreateAsync(string secretName, string secretValue, KeyVaultConfig config, ILogger logger)
        {
            SecretClient secretClient = config.GetClient();

            logger.LogTrace("[Test] create Key vault secret '{SecretName}'", secretName);
            await secretClient.SetSecretAsync(secretName, secretValue);

            return new TemporaryKeyVaultSecret(secretName, secretClient, logger);
        }

        public async Task UpdateSecretAsync(string secretValue)
        {
            _logger.LogTrace("[Test] update Key vault secret '{SecretName}'", _secretName);
            await _client.SetSecretAsync(_secretName, secretValue);
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("[Test] delete Key vault secret '{SecretName}'", _secretName);
            await _client.StartDeleteSecretAsync(_secretName);
        }
    }
}
