﻿using System;
using GuardNet;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration 
{
    /// <summary>
    /// The general options that configures a <see cref="AzureServiceBusMessagePump{TMessage}"/> implementation.
    /// </summary>
    public class AzureServiceBusMessagePumpOptions
    {
        private int? _maxConcurrentCalls;

        /// <summary>
        ///     Maximum concurrent calls to process messages
        /// </summary>
        public int? MaxConcurrentCalls
        {
            get => _maxConcurrentCalls;
            set
            {
                if (value != null)
                {
                    Guard.For<ArgumentException>(() => value <= 0, "Max concurrent calls has to be 1 or above.");
                }

                _maxConcurrentCalls = value;
            }
        }

        /// <summary>
        ///     Indication whether or not messages should be automatically marked as completed if no exceptions occured and
        ///     processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed</remarks>
        public bool AutoComplete { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; set; }
    }
}