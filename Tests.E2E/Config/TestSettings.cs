using PlugIn.Automate.Client;

namespace Tests.E2E.Config
{
    /// <summary>
    /// Loaded from testsettings.json / testsettings.local.json / E2E_* env vars.
    /// Edit testsettings.json to point at your application.
    /// </summary>
    public class TestSettings
    {
        /// <summary>Base URL of the frontend application under test.</summary>
        public string BaseUrl { get; set; } = "http://localhost:3000";

        /// <summary>Base URL of the backend API (used for auth and data seeding).</summary>
        public string ApiUrl { get; set; } = "http://localhost:5000/api";

        /// <summary>Run browsers headlessly. Set to false for local debugging.</summary>
        public bool Headless { get; set; } = true;

        /// <summary>Milliseconds to slow each Playwright action by. Useful when debugging.</summary>
        public int SlowMo { get; set; } = 0;

        /// <summary>
        /// Outer key: pool name ("Pool1", "Pool2").
        /// Inner key: account role name matching <see cref="AutomationAccount"/> enum values.
        /// Each parallel test collection is assigned one pool so tests never share credentials.
        /// </summary>
        public Dictionary<string, Dictionary<string, TestUserSettings>> Pools { get; set; } = new();

        public TestUserSettings GetAccount(string poolName, AutomationAccount account)
        {
            if (!Pools.TryGetValue(poolName, out var pool))
                throw new InvalidOperationException(
                    $"Pool '{poolName}' not found in testsettings.json > Pools.");

            if (!pool.TryGetValue(account.ToString(), out var user))
                throw new InvalidOperationException(
                    $"Account '{account}' not found in pool '{poolName}'. Check testsettings.json.");

            return user;
        }
    }
}
