using System;
using System.Collections.Generic;
using Arcus.Messaging.Pumps.ServiceBus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.KeyRotation
{
    [Trait(name: "Category", value: "Unit")]
    public class AzureServiceBusLocationTests
    {
        public static IEnumerable<object[]> Blanks => new[]
        {
            new object[] { null },
            new object[] { "" },
            new object[] { "  " },
        };

        public static IEnumerable<object[]> OutOfBoundsEntities => new[]
        {
            new object[] { ServiceBusEntityType.Topic | ServiceBusEntityType.Queue },
            new object[] { (ServiceBusEntityType) 3 }
        };

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankResourceGroup_Throws(string resourceGroup)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace(resourceGroup, "namespace", ServiceBusEntityType.Topic, "entity name", "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankNamespace_Throws(string @namespace)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", @namespace, ServiceBusEntityType.Topic, "entity name", "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(OutOfBoundsEntities))]
        public void CreatesLocation_WithBlankOutOfBoundsEntity_Throws(ServiceBusEntityType entity)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", "namespace", entity, "entity name", "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankEntityName_Throws(string entityName)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntityType.Queue, entityName, "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankAuthorizationRuleName_Throw(string authorizationRuleName)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntityType.Queue, "entity name", authorizationRuleName));
        }
    }
}
