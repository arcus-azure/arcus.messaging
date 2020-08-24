using System;
using System.Collections.Generic;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation;
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
            new object[] { ServiceBusEntity.Topic | ServiceBusEntity.Queue },
            new object[] { (ServiceBusEntity) 3 }
        };

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankResourceGroup_Throws(string resourceGroup)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace(resourceGroup, "namespace", ServiceBusEntity.Topic, "entity name", "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankNamespace_Throws(string @namespace)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", @namespace, ServiceBusEntity.Topic, "entity name", "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(OutOfBoundsEntities))]
        public void CreatesLocation_WithBlankOutOfBoundsEntity_Throws(ServiceBusEntity entity)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", "namespace", entity, "entity name", "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankEntityName_Throws(string entityName)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntity.Queue, entityName, "authorization rule name"));
        }

        [Theory]
        [MemberData(nameof(Blanks))]
        public void CreatesLocation_WithBlankAuthorizationRuleName_Throw(string authorizationRuleName)
        {
            Assert.Throws<ArgumentException>(
                () => new AzureServiceBusNamespace("resource group", "namespace", ServiceBusEntity.Queue, "entity name", authorizationRuleName));
        }
    }
}
