using System.Reflection;
using PlugIn.Automate.Client;
using Tests.Api.Live.Config;
using Xunit.Abstractions;

namespace Tests.Api.Live.Fixtures
{
    /// <summary>
    /// Framework base class for live API tests.
    ///
    /// Resolves [UseAccount] from the test class or method, fetches credentials from the
    /// pool, calls LoginAsync to obtain a bearer token, and wires up Anon and Auth clients.
    ///
    /// -- TO GET STARTED --
    /// 1. Open AppLiveApiTestBase.cs and adapt LoginAsync to match your API's auth flow.
    /// 2. Edit apitestsettings.json with your server URL and test credentials.
    /// 3. Write test classes that extend AppLiveApiTestBase (or your own subclass).
    ///    Decorate with [Collection("LiveApi.1")] and [UseAccount(AutomationAccount.SysAdmin)].
    /// </summary>
    public abstract class LiveApiTestBase : IAsyncLifetime
    {
        protected readonly LiveApiFixture Fixture;
        protected LiveApiSettings Settings => Fixture.Settings;

        private readonly ITestOutputHelper _output;

        /// <summary>Anonymous HttpClient -- no Authorization header.</summary>
        protected HttpClient Anon { get; private set; } = null!;

        /// <summary>Authenticated HttpClient -- Bearer token set by LoginAsync before each test.</summary>
        protected HttpClient Auth { get; private set; } = null!;

        protected LiveApiTestBase(LiveApiFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            _output = output;
        }

        public async Task InitializeAsync()
        {
            Anon = ApiClientFactory.CreateAnonymous(Settings.BaseUrl);

            var account = ResolveAccount();
            if (account is not null)
            {
                var user  = Fixture.GetAccount(account.Value);
                var token = await LoginAsync(user);
                Auth = ApiClientFactory.CreateAuthenticated(Settings.BaseUrl, token);
            }
            else
            {
                Auth = ApiClientFactory.CreateAnonymous(Settings.BaseUrl);
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // ── Auth ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Logs in with the supplied credentials and returns the bearer token.
        /// Called automatically when the test is decorated with [UseAccount].
        /// See AppLiveApiTestBase for a ready-to-adapt implementation.
        /// </summary>
        protected abstract Task<string> LoginAsync(TestUserSettings user);

        /// <summary>Returns credentials for the given account from the current pool.</summary>
        protected TestUserSettings GetAccount(AutomationAccount account)
            => Fixture.GetAccount(account);

        // ── Account resolution ────────────────────────────────────────────────────

        private AutomationAccount? ResolveAccount()
        {
            var testField = _output.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => typeof(ITest).IsAssignableFrom(f.FieldType));

            if (testField?.GetValue(_output) is not ITest test)
                return null;

            var methodName = test.TestCase.TestMethod.Method.Name;
            var type       = GetType();
            var method     = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

            return (method?.GetCustomAttribute<UseAccountAttribute>()
                    ?? type.GetCustomAttribute<UseAccountAttribute>())?.Account;
        }
    }
}
