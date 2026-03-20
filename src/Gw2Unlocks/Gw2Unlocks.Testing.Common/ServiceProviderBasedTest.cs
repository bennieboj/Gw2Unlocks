using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Gw2Unlocks.Testing.Common
{
    public abstract class ServiceProviderBasedTest<TSut>
        where TSut : class
    {
        protected ITestOutputHelper XunitTestOutputHelper { get; }

        protected ServiceProviderBasedTest(ITestOutputHelper output) => XunitTestOutputHelper = output;

        private IServiceProvider? _provider;

        /// <summary>
        /// Override to add services to the collection.
        /// </summary>
        protected abstract void Configure(IServiceCollection services);

        /// <summary>
        /// Resolves the System Under Test.
        /// </summary>
        protected TSut GetSut()
        {
            EnsureProvider();
            return _provider!.GetRequiredService<TSut>();
        }

        /// <summary>
        /// Resolves any service.
        /// </summary>
        protected T GetService<T>() where T : class
        {
            EnsureProvider();
            return _provider!.GetRequiredService<T>();
        }

        /// <summary>
        /// Lazily builds the service provider.
        /// </summary>
        private void EnsureProvider()
        {
            if (_provider == null)
            {
                var services = new ServiceCollection();
                services.AddXunitLogging(XunitTestOutputHelper);
                Configure(services);  // safe now — called after derived constructor
                _provider = services.BuildServiceProvider();
            }
        }
    }
}