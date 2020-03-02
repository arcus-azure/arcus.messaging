using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    [Trait("Category", "Unit")]
    public class MessageHandlerTests
    {
        [Fact]
        public void SubtractsMessageHandlers_SelectsAllRegistrations()
        {
            // Arrange
            ServiceDescriptor[] serviceDescriptors = 
            {
                ServiceDescriptor.Singleton(Mock.Of<IMessageHandler<string, MessageContext>>()),
                ServiceDescriptor.Singleton(Mock.Of<IMessageHandler<int, MessageContext>>()),
                ServiceDescriptor.Singleton(Mock.Of<IMessageHandler<TimeSpan, MessageContext>>())
            };

            IServiceProvider serviceProvider = CreateStubServiceProvider(serviceDescriptors);
            
            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider);

            // Assert
            Assert.Equal(3, messageHandlers.Count());
        }

        private static IServiceProvider CreateStubServiceProvider(IEnumerable<ServiceDescriptor> serviceDescriptors)
        {
            var untypedServiceDescriptors = serviceDescriptors.Cast<object>();
            var serviceProviderEngineType = Type.GetType("Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine, Microsoft.Extensions.DependencyInjection");
            Assert.True(serviceProviderEngineType != null, "serviceProviderType != null");

            object serviceProviderEngine = Activator.CreateInstance(serviceProviderEngineType, untypedServiceDescriptors, null);

            var serviceProviderEngineScopeType = Type.GetType("Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope, Microsoft.Extensions.DependencyInjection");
            Assert.True(serviceProviderEngineScopeType != null, "serviceProviderEngineScopeType != null");
            
            return (IServiceProvider) Activator.CreateInstance(serviceProviderEngineScopeType, serviceProviderEngine);
        }
    }
}
