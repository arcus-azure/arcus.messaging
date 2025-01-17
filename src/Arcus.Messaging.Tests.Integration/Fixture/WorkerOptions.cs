using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Configuration;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents the configurable options to influence the test <see cref="Worker"/>.
    /// </summary>
    public class WorkerOptions : IServiceCollection
    {
        private ITestOutputHelper _outputWriter;
        private readonly ICollection<Action<LoggerConfiguration>> _additionalSerilogConfigOptions = new Collection<Action<LoggerConfiguration>>();
        private readonly ICollection<Action<IHostBuilder>> _additionalHostOptions = new Collection<Action<IHostBuilder>>();

        /// <summary>
        /// Gets the services that will be included in the test <see cref="Worker"/>.
        /// </summary>
        public IServiceCollection Services { get; } = new ServiceCollection();

        /// <summary>
        /// Gets the configuration instance that will be included in the test <see cref="Worker"/> and which will result in an <see cref="IConfiguration"/> instance.
        /// </summary>
        public IDictionary<string, string> Configuration { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Adds an additional configuration option on the to-be-created <see cref="IHostBuilder"/>.
        /// </summary>
        /// <param name="additionalHostOption">The action that configures the additional option.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="additionalHostOption"/> is <c>null</c>.</exception>
        public WorkerOptions Configure(Action<IHostBuilder> additionalHostOption)
        {
            Guard.NotNull(additionalHostOption, nameof(additionalHostOption), "Requires an custom action that will add the additional hosting option");
            _additionalHostOptions.Add(additionalHostOption);

            return this;
        }

        /// <summary>
        /// Adds a <paramref name="logger"/> to the test worker instance to write diagnostic trace messages to the test output.
        /// </summary>
        /// <param name="logger">The test logger to write the diagnostic trace messages to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public WorkerOptions AddXunitTestLogging(ITestOutputHelper logger)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic trace messages to the test output");

            _outputWriter = logger;
            return this;
        }

        /// <summary>
        /// Add a function to configure the Serilog logging in the test worker.
        /// </summary>
        /// <param name="configure">The function to configure the Serilog configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configure"/> is <c>null</c>.</exception>
        public WorkerOptions ConfigureSerilog(Action<LoggerConfiguration> configure)
        {
            Guard.NotNull(configure, nameof(configure));

            _additionalSerilogConfigOptions.Add(configure);
            return this;
        }

        /// <summary>
        /// Applies the previously configured additional host options to the given <paramref name="hostBuilder"/>.
        /// </summary>
        /// <param name="hostBuilder">The builder instance to apply the additional host options to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="hostBuilder"/> is <c>null</c>.</exception>
        internal void ApplyOptions(IHostBuilder hostBuilder)
        {
            Guard.NotNull(hostBuilder, nameof(hostBuilder), "Requires a host builder instance to apply the worker options to");

            hostBuilder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(Configuration))
                       .ConfigureServices(services =>
                       {
                           foreach (ServiceDescriptor service in Services)
                           {
                               services.Add(service);
                           }
                       })
                       .UseSerilog((context, config) =>
                       {
                           config.MinimumLevel.Verbose()
                                 .Enrich.FromLogContext();

                           if (_outputWriter != null)
                           {
                               config.WriteTo.XunitTestLogging(_outputWriter);
                           }

                           foreach (Action<LoggerConfiguration> configure in _additionalSerilogConfigOptions)
                           {
                               configure(config);
                           }
                       });

            foreach (Action<IHostBuilder> additionalHostOption in _additionalHostOptions)
            {
                additionalHostOption(hostBuilder);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return Services.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Services).GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public void Add(ServiceDescriptor item)
        {
            Services.Add(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public void Clear()
        {
            Services.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />.</returns>
        public bool Contains(ServiceDescriptor item)
        {
            return Services.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex" /> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            Services.CopyTo(array, arrayIndex);
        }

        /// <summary>Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
        public bool Remove(ServiceDescriptor item)
        {
            return Services.Remove(item);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public int Count => Services.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, <see langword="false" />.</returns>
        public bool IsReadOnly => Services.IsReadOnly;

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
        public int IndexOf(ServiceDescriptor item)
        {
            return Services.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
        public void Insert(int index, ServiceDescriptor item)
        {
            Services.Insert(index, item);
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
        public void RemoveAt(int index)
        {
            Services.RemoveAt(index);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1" />.</exception>
        /// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1" /> is read-only.</exception>
        public ServiceDescriptor this[int index]
        {
            get => Services[index];
            set => Services[index] = value;
        }
    }
}