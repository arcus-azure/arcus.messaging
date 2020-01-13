using System;
using System.Text;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arcus.Messaging.Health.Tcp
{
    /// <summary>
    /// Options to control how the <see cref="TcpHealthListener"/> acts as a TCP endpoint for the <see cref="HealthReport"/>.
    /// </summary>
    public class TcpHealthListenerOptions
    {
        private const string TcpHealthPort = "ARCUS_HEALTH_PORT";

        private static readonly JsonSerializerSettings SerializationSettings = CreateDefaultSerializerSettings();

        private Func<HealthReport, byte[]> _reportSerializer = report =>
        {
            string json = JsonConvert.SerializeObject(report, SerializationSettings);
            byte[] response = Encoding.UTF8.GetBytes(json);

            return response;
        };

        private Func<IServiceProvider, int> _getTcpHealthPort = serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            string tcpPortString = configuration[TcpHealthPort];
            
            if (!Int32.TryParse(tcpPortString, out int tcpPort))
            {
                throw new ArithmeticException(
                    $"Requires a configuration implementation with a '{TcpHealthPort}' key containing a TCP port number");
            }

            return tcpPort;
        };

        private static JsonSerializerSettings CreateDefaultSerializerSettings()
        {
            var serializingSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None, 
                NullValueHandling = NullValueHandling.Ignore
            };

            var enumConverter = new StringEnumConverter { AllowIntegerValues = false };
            serializingSettings.Converters.Add(enumConverter);

            return serializingSettings;
        }

        /// <summary>
        /// Gets or sets the function that serializes the <see cref="HealthReport"/> to a series of bytes.
        /// </summary>
        public Func<HealthReport, byte[]> ReportSerializer
        {
            get => _reportSerializer;
            set
            {
                Guard.NotNull(value, nameof(value), "Cannot set a 'null' health report serializer for the TCP listener");
                _reportSerializer = value;
            }
        }

        /// <summary>
        /// Gets or sets the function that gets the TCP health check port, using the service object with a registered collection of instances.
        /// </summary>
        public Func<IServiceProvider, int> GetTcpHealthPort
        {
            get => _getTcpHealthPort;
            set
            {
                Guard.NotNull(value, nameof(value), "Cannot set a 'null' function to get the TCP health check port");
                _getTcpHealthPort = value;
            }
        }
    }
}
