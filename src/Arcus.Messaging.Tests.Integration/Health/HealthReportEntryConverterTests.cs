using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.Health
{
    public class HealthReportEntryConverterTests
    {
        [Fact]
        public void Deserialize_HealthReportWithoutException_Succeeds()
        {
            // Arrange
            string json = 
                @"{""Entries"":
                    {""sample"":
                        {""Data"":{""key"":""value""},
                         ""Description"":""desc"",
                         ""Duration"":""00:00:05"",
                         ""Status"":2,
                         ""Tags"":[""tag1""]}},""Status"":2,""TotalDuration"":""00:00:05""}";
            

            // Act
            var report = JsonConvert.DeserializeObject<HealthReport>(json, new HealthReportEntryConverter());
            
            // Assert
            Assert.NotNull(report);
            Assert.Equal(HealthStatus.Healthy, report.Status);
            
            (string entryName, HealthReportEntry entry) = Assert.Single(report.Entries);
            Assert.Equal("sample", entryName);
            Assert.Equal(HealthStatus.Healthy, entry.Status);
            Assert.Equal("desc", entry.Description);
            
            (string key, object data) = Assert.Single(entry.Data);
            Assert.Equal("key", key);
            Assert.Equal("value", data);
            Assert.Equal(TimeSpan.FromSeconds(5), entry.Duration);
            
            string tag = Assert.Single(entry.Tags);
            Assert.Equal("tag1", tag);
        }
    }
}
