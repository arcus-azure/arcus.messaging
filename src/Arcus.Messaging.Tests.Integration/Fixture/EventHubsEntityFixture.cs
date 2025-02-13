using System;
using System.Threading.Tasks;
using Arcus.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class EventHubsEntityFixture : IAsyncLifetime
    {
        private TemporaryEventHubEntity _hub;

        public string HubName { get; } = $"hub-{Guid.NewGuid()}";

        public async Task InitializeAsync()
        {
            var config = TestConfig.Create();
            _hub = await TemporaryEventHubEntity.CreateAsync(HubName, config.GetEventHubs(), NullLogger.Instance);
        }

        public async Task DisposeAsync()
        {
            await _hub.DisposeAsync();
        }
    }
}
