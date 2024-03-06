using System.Threading.Tasks;
using Arcus.Security.Core;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    internal class AsyncOnlyInMemorySecretProvider : ISecretProvider
    {
        private readonly string _secretName;
        private readonly string _secretValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOnlyInMemorySecretProvider" /> class.
        /// </summary>
        public AsyncOnlyInMemorySecretProvider(string secretName, string secretValue)
        {
            _secretName = secretName;
            _secretValue = secretValue;
        }

        public async Task<Secret> GetSecretAsync(string secretName)
        {
            string secretValue = await GetRawSecretAsync(secretName);
            if (secretValue is null)
            {
                return null;
            }

            return new Secret(secretValue);
        }

        public Task<string> GetRawSecretAsync(string secretName)
        {
            if (secretName == _secretName)
            {
                return Task.FromResult(_secretValue);
            }

            return null;
        }
    }
}