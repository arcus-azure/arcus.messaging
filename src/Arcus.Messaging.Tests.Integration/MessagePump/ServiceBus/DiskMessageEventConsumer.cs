using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Workers.ServiceBus.Fixture;
using Arcus.Testing;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    public static class DiskMessageEventConsumer
    {
        private static readonly JsonSerializerOptions MessageCorrelationInfoJsonOptions = CreateMessageCorrelationInfoJsonOptionsJsonOptions();

        public static async Task<OrderCreatedEventData> ConsumeOrderCreatedAsync(string messageId, TimeSpan? timeout = null)
        {
            return await ConsumeEventAsync<OrderCreatedEventData>(messageId,
                $"order created event does not seem to be delivered in time as the file '{messageId}.json' cannot be found on disk", timeout);
        }

        private static async Task<TResult> ConsumeEventAsync<TResult>(string messageId, string errorMessage, TimeSpan? timeout = null)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            FileInfo file =
                await Poll.Target(() => Assert.Single(dir.GetFiles($"{messageId}.json", SearchOption.AllDirectories)))
                          .Until(files => files.Length > 0)
                          .Every(TimeSpan.FromMilliseconds(100))
                          .Timeout(timeout ?? TimeSpan.FromSeconds(15))
                          .FailWith(errorMessage);

            string json = await File.ReadAllTextAsync(file.FullName);
            var eventData = JsonSerializer.Deserialize<TResult>(
                json,
                MessageCorrelationInfoJsonOptions);

            return eventData;
        }

        private static JsonSerializerOptions CreateMessageCorrelationInfoJsonOptionsJsonOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new MessageCorrelationInfoJsonConverter());
            return options;
        }
    }
}