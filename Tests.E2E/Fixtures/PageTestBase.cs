using System.Reflection;
using Microsoft.Playwright;
using PlugIn.Automate.Client;
using Tests.E2E.Config;
using Xunit.Abstractions;

namespace Tests.E2E.Fixtures
{
    /// <summary>
    /// Base class for all Playwright page tests.
    ///
    /// Handles browser context/page lifecycle, Playwright tracing, and automatic login
    /// when a test is decorated with [UseAccount].
    ///
    /// -- TO GET STARTED --
    /// 1. Open AppPageTestBase.cs and implement LoginAsync for your application's auth flow.
    /// 2. Update testsettings.json with your app's BaseUrl, ApiUrl, and credentials.
    /// 3. Write test classes that extend AppPageTestBase (or your own subclass).
    /// </summary>
    public abstract class PageTestBase : IAsyncLifetime
    {
        protected readonly PlaywrightFixture Fixture;
        protected IBrowserContext Context { get; private set; } = null!;
        protected IPage Page { get; private set; } = null!;
        protected TestSettings Settings => Fixture.Settings;

        private readonly ITestOutputHelper _output;
        private string _testName = "unknown";

        private static readonly string TracesDir =
            Path.Combine(AppContext.BaseDirectory, "traces");

        protected PageTestBase(PlaywrightFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            _output = output;
        }

        public virtual async Task InitializeAsync()
        {
            Context = await Fixture.NewContextAsync();
            Page    = await Context.NewPageAsync();

            var (account, testName) = ResolveTestInfo();
            _testName = testName;

            Directory.CreateDirectory(TracesDir);
            await Context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots   = true,
                Sources     = true,
            });

            if (account is not null)
                await LoginAsync(Fixture.GetAccount(account.Value));
        }

        public virtual async Task DisposeAsync()
        {
            await Context.Tracing.StopAsync(new()
            {
                Path = Path.Combine(TracesDir, $"{_testName}.zip"),
            });
            await Context.CloseAsync();
        }

        // ── Auth helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Implements your application's login flow.
        /// Called automatically when the test is decorated with [UseAccount].
        /// See AppPageTestBase for a ready-to-adapt JWT + localStorage implementation.
        /// </summary>
        protected abstract Task LoginAsync(TestUserSettings account);

        /// <summary>
        /// Ensures the account exists in the system without touching the browser session.
        /// Override in AppPageTestBase when auth UI tests need the account to pre-exist.
        /// Default: no-op.
        /// </summary>
        protected virtual Task EnsureAccountExistsAsync(AutomationAccount account)
        {
            return Task.CompletedTask;
        }

        /// <summary>Returns credentials for the given account from the current pool.</summary>
        protected TestUserSettings GetAccount(AutomationAccount account)
        {
            return Fixture.GetAccount(account);
        }

        // ── Test info resolution ──────────────────────────────────────────────────

        private (AutomationAccount? account, string testName) ResolveTestInfo()
        {
            var testField = _output.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => typeof(ITest).IsAssignableFrom(f.FieldType));

            if (testField?.GetValue(_output) is not ITest test)
                return (null, "unknown");

            var methodName = test.TestCase.TestMethod.Method.Name;
            var type       = GetType();
            var method     = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

            var account = (method?.GetCustomAttribute<UseAccountAttribute>()
                           ?? type.GetCustomAttribute<UseAccountAttribute>())?.Account;

            return (account, methodName);
        }
    }
}
