using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class FallbackMessageHandlerTests
    {
        [Fact]
        public async Task Add_WithJobId_AdaptsContextFilter()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var services = new MessageHandlerCollection(new ServiceCollection())
            {
                JobId = jobId
            };

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var handler = provider.GetRequiredService<FallbackMessageHandler<string, MessageContext>>();

            var message = "message-body";
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            var correctContext = new MessageContext("message-id", jobId, new Dictionary<string, object>());
            Assert.True(await handler.ProcessMessageAsync(message, correctContext, correlationInfo, CancellationToken.None));

            var incorrectContext = new MessageContext("message-id", "other-job-id", new Dictionary<string, object>());
            Assert.False(await handler.ProcessMessageAsync(message, incorrectContext, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task AddT_WithJobId_AdaptsContextFilter()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var services = new MessageHandlerCollection(new ServiceCollection())
            {
                JobId = jobId
            };

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler<TestMessageContext>, TestMessageContext>();

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var handler = provider.GetRequiredService<FallbackMessageHandler<string, TestMessageContext>>();

            var message = "message-body";
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");

            var correctContext = new TestMessageContext("message-id", jobId, new Dictionary<string, object>());
            Assert.True(await handler.ProcessMessageAsync(message, correctContext, correlationInfo, CancellationToken.None));

            var incorrectContext = new TestMessageContext("message-id", "other-job-id", new Dictionary<string, object>());
            Assert.False(await handler.ProcessMessageAsync(message, incorrectContext, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task Process_WithoutMessage_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var handler = provider.GetRequiredService<FallbackMessageHandler<string, MessageContext>>();

            var messageContext = new MessageContext("message-id", new Dictionary<string, object>());
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => handler.ProcessMessageAsync(message: null, messageContext, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task Process_WithoutMessageContext_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var handler = provider.GetRequiredService<FallbackMessageHandler<string, MessageContext>>();

            var message = "message-body";
            var correlationInfo = new MessageCorrelationInfo("operation-id", "transaction-id");
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => handler.ProcessMessageAsync(message, messageContext: null, correlationInfo, CancellationToken.None));
        }

        [Fact]
        public async Task Process_WithoutMessageCorrelation_Fails()
        {
            // Arrange
            var services = new MessageHandlerCollection(new ServiceCollection());

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.Services.BuildServiceProvider();
            var handler = provider.GetRequiredService<FallbackMessageHandler<string, MessageContext>>();

            var message = "message-body";
            var messageContext = new MessageContext("message-id", new Dictionary<string, object>());
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => handler.ProcessMessageAsync(message, messageContext, correlationInfo: null, CancellationToken.None));
        }
    }
}
