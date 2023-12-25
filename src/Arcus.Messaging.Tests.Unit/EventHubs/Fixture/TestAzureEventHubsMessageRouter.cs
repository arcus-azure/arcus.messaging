using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;

namespace Arcus.Messaging.Tests.Unit.EventHubs.Fixture
{
    public class TestAzureEventHubsMessageRouter : AzureEventHubsMessageRouter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestAzureEventHubsMessageRouter" /> class.
        /// </summary>
        public TestAzureEventHubsMessageRouter(IServiceProvider provider) : base(provider)
        {
        }
    }
}
