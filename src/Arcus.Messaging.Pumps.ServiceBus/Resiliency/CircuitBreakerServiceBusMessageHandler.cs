using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus.Resiliency
{


    /// <summary>
    /// 
    /// </summary>
    public abstract class CircuitBreakerServiceBusMessageHandler<TMessage> : AzureServiceBusMessageHandler<TMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerServiceBusMessageHandler{TMessage}" /> class.
        /// </summary>
        public CircuitBreakerServiceBusMessageHandler(
            IMessagePumpCircuitBreaker circuitBreaker,
            ILogger<CircuitBreakerServiceBusMessageHandler<TMessage>> logger) 
            : base(logger)
        {
            CircuitBreaker = circuitBreaker;
        }

        /// <summary>
        /// Gets the circuit breaker that controls the activation of the message pump.
        /// </summary>
        protected IMessagePumpCircuitBreaker CircuitBreaker { get; }

        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        public override async Task ProcessMessageAsync(
            TMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var options = new MessagePumpCircuitBreakerOptions();
            MessageProcessingResult result = await TryProcessMessageAsync(message, messageContext, correlationInfo, options, cancellationToken);

            if (!result.IsSuccessful)
            {
                await CircuitBreaker.PauseMessageProcessingAsync(messageContext.JobId, opt =>
                {
                    opt.MessageIntervalDuringRecovery = options.MessageIntervalDuringRecovery;
                    opt.MessageRecoveryPeriod = options.MessageRecoveryPeriod;
                });
                await AbandonMessageAsync();
                throw result.ProcessingException;
            }

            await CircuitBreaker.ResumeMessageProcessingAsync(messageContext.JobId);
        }

        private async Task<MessageProcessingResult> TryProcessMessageAsync(
            TMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            MessagePumpCircuitBreakerOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                await ProcessMessageAsync(message, messageContext, correlationInfo, options, cancellationToken);
                return MessageProcessingResult.Success;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Circuit breaker triggered due to thrown exception: {Message}", exception.Message);
                return MessageProcessingResult.Failure(exception);
            }
        }

        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="options">The additional options to manipulate the possible circuit breakage of the message pump for which a message is processed.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        protected abstract Task ProcessMessageAsync(
            TMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            MessagePumpCircuitBreakerOptions options,
            CancellationToken cancellationToken);
    }
}
