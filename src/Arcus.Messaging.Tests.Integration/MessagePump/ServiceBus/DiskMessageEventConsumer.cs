using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Workers.ServiceBus.Fixture;
using Arcus.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    public static class DiskMessageEventConsumer
    {
        public static async Task<OrderCreatedEventData> ConsumeOrderCreatedAsync(string messageId)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            FileInfo file = 
                await Poll.Target(() => Assert.Single(dir.GetFiles($"{messageId}.json", SearchOption.AllDirectories)))
                          .Until(files => files.Length > 0)
                          .Every(TimeSpan.FromMilliseconds(100))
                          .Timeout(TimeSpan.FromSeconds(5))
                          .FailWith($"order created event does not seem to be delivered in time as the file '{messageId}.json' cannot be found on disk");

            string json = await File.ReadAllTextAsync(file.FullName);
            var eventData = JsonConvert.DeserializeObject<OrderCreatedEventData>(json, new MessageCorrelationInfoJsonConverter());

            return eventData;
        }
    }
}