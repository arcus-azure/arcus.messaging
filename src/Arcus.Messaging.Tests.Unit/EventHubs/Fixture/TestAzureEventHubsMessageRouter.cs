using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.EventHubs.Fixture
{
    public class TestAzureEventHubsMessageRouter : AzureEventHubsMessageRouter
    {
        public TestAzureEventHubsMessageRouter(
            IServiceProvider serviceProvider, 
            AzureEventHubsMessageRouterOptions options, 
            ILogger<AzureEventHubsMessageRouter> logger) 
            : base(serviceProvider, options, logger)
        {
        }
    }
}
