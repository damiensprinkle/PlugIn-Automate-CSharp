using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using PlugIn.Automate.Client;
using Tests.E2E.Config;

namespace Tests.E2E.Fixtures
{
    /// <summary>
    /// Shared xUnit fixture that owns the Playwright browser process for a test collection.
    /// Each test class gets a fresh isolated BrowserContext (separate cookies/storage).
    ///
    /// Two concrete pools are provided out of the box (Pool1 / Pool2) so parallel
    /// test collections never share credentials. Add more pools to testsettings.json
    /// and create additional subclasses here if you need more parallelism.
    /// </summary>
    public abstract class PlaywrightFixture : IAsyncLifetime
    {
        public IPlaywright Playwright { get; private set; } = null!;
        public IBrowser Browser { get; private set; } = null!;
        public TestSettings Settings { get; private set; } = null!;

        protected abstract string PoolName { get; }

        public async Task InitializeAsync()
        {
            Settings = LoadSettings();

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = Settings.Headless,
                SlowMo   = Settings.SlowMo,
            });
        }

        public async Task DisposeAsync()
        {
            await Browser.CloseAsync();
            Playwright.Dispose();
        }

        public async Task<IBrowserContext> NewContextAsync()
        {
            return await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL      = Settings.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            });
        }

        public TestUserSettings GetAccount(AutomationAccount account)
        {
            return Settings.GetAccount(PoolName, account);
        }

        private static TestSettings LoadSettings()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("testsettings.json", optional: false)
                .AddJsonFile("testsettings.local.json", optional: true)
                .AddEnvironmentVariables("E2E_")
                .Build();

            var settings = new TestSettings();
            config.Bind(settings);
            return settings;
        }
    }

    public class PlaywrightFixturePool1 : PlaywrightFixture
    {
        protected override string PoolName => "Pool1";
    }

    public class PlaywrightFixturePool2 : PlaywrightFixture
    {
        protected override string PoolName => "Pool2";
    }
}
