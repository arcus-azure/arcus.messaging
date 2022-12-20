using System.Threading.Tasks;
using Arcus.Security.Core;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    internal class AsyncInMemorySecretProvider : ISecretProvider
    {
        private readonly string _secretName;
        private readonly string _secretValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncInMemorySecretProvider" /> class.
        /// </summary>
        public AsyncInMemorySecretProvider(string secretName, string secretValue)
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