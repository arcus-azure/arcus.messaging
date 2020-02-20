using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents an CLI command argument that can safely be used when passing secrets to an application.
    /// </summary>
    public class CommandArgument
    {
        private readonly string _value;
        private readonly bool _isSecret;

        private CommandArgument(string name, string value, bool isSecret)
        {
            Name = name;
            _value = value;
            _isSecret = isSecret;
        }

        /// <summary>
        /// Gets the name of the CLI argument.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the exposed command argument name and value; including private information; use with care.
        /// </summary>
        /// <remarks>
        ///     This method will return the secrets embedded, please do not use it in logging or other external sources.
        /// </remarks>
        internal string ToExposedString()
        {
            return $"--{Name} {_value}";
        }

        /// <summary>
        /// Create an CLI command argument with a secret value (i.e. access key).
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="secret">The secret value of the argument.</param>
        public static CommandArgument CreateSecret(string name, object secret)
        {
            Guard.NotNullOrWhitespace(name, nameof(name), "Name of CLI command argument cannot be blank");
            Guard.NotNull(secret, nameof(secret), "Value of CLI command argument cannot be 'null'");
            string valueString = secret.ToString();
            Guard.NotNullOrWhitespace(valueString, nameof(secret), "Value of CLI command argument cannot be blank");

            return new CommandArgument(name, valueString, isSecret: true);
        }

        /// <summary>
        /// Creates an CLI command argument with a open value (i.e. health port number).
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="value">The open value of the argument.</param>
        public static CommandArgument CreateOpen(string name, object value)
        {
            Guard.NotNullOrWhitespace(name, nameof(name), "Name of CLI command argument cannot be blank");
            Guard.NotNull(value, nameof(value), "Value of CLI command argument cannot be 'null'");
            string valueString = value.ToString();
            Guard.NotNullOrWhitespace(valueString, nameof(value), "Value of CLI command argument cannot be blank");

            return new CommandArgument(name, valueString, isSecret: false);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _isSecret ? $"--{Name} ***" : $"--{Name} {_value}";
        }
    }
}
