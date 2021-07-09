using System;
using System.Collections.Generic;
using System.Text;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.AzureFunctions
{
    // ReSharper disable once InconsistentNaming
    public class IFunctionsHostBuilderExtensionsTests
    {
        [Fact]
        public void AddServiceBusRouting_RegistersRouter_GetsRouterSucceeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.AddServiceBusMessageRouting();
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IAzureServiceBusMessageRouter>());
        }
        
        [Fact]
        public void AddServiceBusRoutingT_RegistersRouter_GetsRouterSucceeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(b => b.Services).Returns(services);
            
            // Act
            builder.Object.AddServiceBusMessageRouting(serviceProvider =>
            {
                return new TestAzureServiceBusMessageRouter(serviceProvider, NullLogger.Instance);
            });
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var router = provider.GetService<IAzureServiceBusMessageRouter>();
            Assert.NotNull(router);
            Assert.IsType<TestAzureServiceBusMessageRouter>(router);
        }
    }
}
