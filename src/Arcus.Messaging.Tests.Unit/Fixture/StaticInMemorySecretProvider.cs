using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    internal class StaticInMemorySecretProvider : ISyncSecretProvider
    {
        private readonly string _secretName;
        private readonly string _secretValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticInMemorySecretProvider" /> class.
        /// </summary>
        public StaticInMemorySecretProvider(string secretName, string secretValue)
        {
            _secretName = secretName;
            _secretValue = secretValue;
        }

        public Task<string> GetRawSecretAsync(string secretName)
        {
            return Task.FromResult(GetRawSecret(secretName));
        }

        public Task<Secret> GetSecretAsync(string secretName)
        {
            return Task.FromResult(GetSecret(secretName));
        }

        public string GetRawSecret(string secretName)
        {
            if (secretName == _secretName)
            {
                return _secretValue;
            }

            return null;
        }

        public Secret GetSecret(string secretName)
        {
            string secretValue = GetRawSecret(secretName);
            if (secretValue is null)
            {
                return null;
            }

            return new Secret(secretValue);
        }
    }
}
