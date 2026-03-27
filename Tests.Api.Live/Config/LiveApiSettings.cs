using PlugIn.Automate.Client;

namespace Tests.Api.Live.Config
{
    /// <summary>
    /// Loaded from apitestsettings.json / apitestsettings.local.json / API_* env vars.
    /// Edit apitestsettings.json to point at your running API server.
    /// </summary>
    public class LiveApiSettings
    {
        /// <summary>Base URL of the API server, e.g. "http://localhost:5000".</summary>
        public string BaseUrl { get; set; } = "http://localhost:5000";

        /// <summary>
        /// Outer key: pool name ("Pool1", "Pool2").
        /// Inner key: account role matching <see cref="AutomationAccount"/> enum values.
        /// Each parallel test collection is assigned one pool so tests never share accounts.
        /// </summary>
        public Dictionary<string, Dictionary<string, TestUserSettings>> Pools { get; set; } = new();

        public TestUserSettings GetAccount(string poolName, AutomationAccount account)
        {
            if (!Pools.TryGetValue(poolName, out var pool))
                throw new InvalidOperationException(
                    $"Pool '{poolName}' not found in apitestsettings.json > Pools.");

            if (!pool.TryGetValue(account.ToString(), out var user))
                throw new InvalidOperationException(
                    $"Account '{account}' not found in pool '{poolName}'. Check apitestsettings.json.");

            return user;
        }
    }
}
