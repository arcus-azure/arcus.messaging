using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class EmptyMessagePump : AzureServiceBusMessagePump
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        public EmptyMessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger logger) : base(configuration, serviceProvider, logger) { }
    }
}
