using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class EventHubsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        public EventHubsConfig(string eventHubsName, string connectionString, string storageConnectionString)
        {
            EventHubsName = eventHubsName;
            EventHubsConnectionString = connectionString;
            StorageConnectionString = storageConnectionString;
        }

        public string EventHubsName { get; }

        public string EventHubsConnectionString { get; }

        public string StorageConnectionString { get; }
    }
}
