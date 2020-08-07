using System;
using System.Collections.Generic;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation;
using Arcus.Security.Core;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.KeyRotation
{
    [Trait(name: "Category", value: "Unit")]
    public class AzureServiceBusManagementAuthenticationTests
    {
        public static IEnumerable<object[]> Blanks => new[]
        {
            new object[] { null },
            new object[] { "" },
            new object[] { "  " }
        };

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesAuthentication_WithBlankClientId_Throws(string clientId)
        {
            Assert.Throws<ArgumentException>(
                () => new DefaultAzureServiceBusManagementAuthentication(
                    clientId, "client secret key", "subscription ID", "tenant ID", Mock.Of<ISecretProvider>()));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesAuthentication_WithBlankClientSecretKey_Throws(string clientSecretKey)
        {
            Assert.Throws<ArgumentException>(
                () => new DefaultAzureServiceBusManagementAuthentication(
                    "client ID", clientSecretKey, " subscription ID", "tenant ID", Mock.Of<ISecretProvider>()));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesAuthentication_WithBlankSubscriptionId_Throws(string subscriptionId)
        {
            Assert.Throws<ArgumentException>(
                () => new DefaultAzureServiceBusManagementAuthentication(
                    "client ID", "client secret key", subscriptionId, "tenant ID", Mock.Of<ISecretProvider>()));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesAuthentication_WithBlankTenantId_Throws(string tenantId)
        {
            Assert.Throws<ArgumentException>(
                () => new DefaultAzureServiceBusManagementAuthentication(
                    "client ID", "client secret key", "subscription ID", tenantId, Mock.Of<ISecretProvider>()));
        }

        [Fact]
        public void CreatesAuthentication_WithoutSecretProvider_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new DefaultAzureServiceBusManagementAuthentication(
                    "client ID", "client secret key", "subscription ID", "tenant ID", secretProvider: null));
        }
    }
}
