using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.AzureFunctions.EventHubs;
using Arcus.Messaging.Tests.Unit.EventHubs.Fixture;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs.AzureFunctions
{
    // ReSharper disable once InconsistentNaming
    public class IFunctionsHostBuilderExtensionsTests
    {
        [Fact]
        public void AddEventHubsMessageRouting_WithoutOptions_RegistersRouter()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.AddEventHubsMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IAzureEventHubsMessageRouter>());
            Assert.NotNull(provider.GetService<AzureFunctionsInProcessMessageCorrelation>());
        }

        [Fact]
        public void AddEventHubsRouting_WithOptions_RegistersRouter()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.AddEventHubsMessageRouting(configureOptions: options => { });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IAzureEventHubsMessageRouter>());
            Assert.NotNull(provider.GetService<AzureFunctionsInProcessMessageCorrelation>());
        }

        [Fact]
        public void AddEventHubsRoutingT_WithoutOptions_RegistersRouter()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.AddEventHubsMessageRouting(
                serviceProvider => new TestAzureEventHubsMessageRouter(serviceProvider));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var messageRouter = provider.GetService<IAzureEventHubsMessageRouter>();
            Assert.NotNull(messageRouter);
            Assert.IsType<TestAzureEventHubsMessageRouter>(messageRouter);
            Assert.NotNull(provider.GetService<AzureFunctionsInProcessMessageCorrelation>());
        }

        [Fact]
        public void AddEventHubsRoutingT_WithOptions_RegistersRouter()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.AddEventHubsMessageRouting(
                (serviceProvider, options) => new TestAzureEventHubsMessageRouter(serviceProvider),
                configureOptions: null);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var messageRouter = provider.GetService<IAzureEventHubsMessageRouter>();
            Assert.NotNull(messageRouter);
            Assert.IsType<TestAzureEventHubsMessageRouter>(messageRouter);
            Assert.NotNull(provider.GetService<AzureFunctionsInProcessMessageCorrelation>());
        }
    }
}
