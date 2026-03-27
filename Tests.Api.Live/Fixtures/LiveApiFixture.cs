using Microsoft.Extensions.Configuration;
using PlugIn.Automate.Client;
using Tests.Api.Live.Config;

namespace Tests.Api.Live.Fixtures
{
    /// <summary>
    /// Shared xUnit fixture that loads settings and provides account resolution
    /// for a single parallel test collection.
    ///
    /// Two concrete pools are provided (Pool1 / Pool2) so parallel test collections
    /// never share credentials. Add more pools to apitestsettings.json and create
    /// additional subclasses if you need more parallelism.
    /// </summary>
    public abstract class LiveApiFixture : IAsyncLifetime
    {
        public LiveApiSettings Settings { get; private set; } = null!;

        protected abstract string PoolName { get; }

        public Task InitializeAsync()
        {
            Settings = LoadSettings();
            return Task.CompletedTask;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public TestUserSettings GetAccount(AutomationAccount account)
            => Settings.GetAccount(PoolName, account);

        private static LiveApiSettings LoadSettings()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("apitestsettings.json", optional: false)
                .AddJsonFile("apitestsettings.local.json", optional: true)
                .AddEnvironmentVariables("API_")
                .Build();

            var settings = new LiveApiSettings();
            config.Bind(settings);
            return settings;
        }
    }

    public class LiveApiFixturePool1 : LiveApiFixture
    {
        protected override string PoolName => "Pool1";
    }

    public class LiveApiFixturePool2 : LiveApiFixture
    {
        protected override string PoolName => "Pool2";
    }
}
