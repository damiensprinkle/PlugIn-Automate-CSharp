using PlugIn.Automate.Client;

namespace Tests.Api.Fixtures
{
    /// <summary>
    /// Base class for all API integration tests.
    /// Provides an anonymous HttpClient and an authenticated one, both wrapping the
    /// in-process ApiFactory server. Auth setup runs before each test via InitializeAsync.
    ///
    /// -- TO GET STARTED --
    /// Adapt the auth block in InitializeAsync to match your login endpoint and response shape.
    /// Then write test classes that extend this base.
    /// </summary>
    [Collection("Api")]
    public abstract class ApiTestBase : IAsyncLifetime
    {
        protected readonly ApiFactory Factory;

        protected const string TestEmail    = "test@example.com";
        protected const string TestPassword = "Pa$$w0rd";
        protected const string TestUsername = "testuser";
        protected const string TestDisplay  = "Test User";

        /// <summary>Anonymous HttpClient -- no Authorization header.</summary>
        protected HttpClient Anon { get; private set; } = null!;

        /// <summary>Authenticated HttpClient -- Bearer token injected before each test.</summary>
        protected HttpClient Auth { get; private set; } = null!;

        protected ApiTestBase(ApiFactory factory)
        {
            Factory = factory;
        }

        public async Task InitializeAsync()
        {
            Anon = Factory.CreateClient();

            // Register the test user (idempotent -- ignore errors if already exists)
            // Adapt the endpoint path and body shape to match your API.
            try
            {
                var registerBody = System.Text.Json.JsonSerializer.Serialize(new
                {
                    email       = TestEmail,
                    password    = TestPassword,
                    username    = TestUsername,
                    displayName = TestDisplay,
                });
                await Anon.PostAsync("/api/account/register",
                    new StringContent(registerBody, System.Text.Encoding.UTF8, "application/json"));
            }
            catch { /* already exists */ }

            // Log in and extract the token.
            // Adapt the endpoint path and JSON property name to match your API.
            var loginBody = System.Text.Json.JsonSerializer.Serialize(new
            {
                email    = TestEmail,
                password = TestPassword,
            });
            var loginResp = await Anon.PostAsync("/api/account/login",
                new StringContent(loginBody, System.Text.Encoding.UTF8, "application/json"));

            loginResp.EnsureSuccessStatusCode();

            var loginJson = await loginResp.Content.ReadAsStringAsync();
            var doc       = System.Text.Json.JsonDocument.Parse(loginJson);
            var token     = doc.RootElement.GetProperty("token").GetString()!;

            Auth = ApiClientFactory.CreateForFactory(Factory.CreateClient(), token);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>Creates a fresh anonymous HttpClient wrapping the in-process factory.</summary>
        protected HttpClient CreateClient() => Factory.CreateClient();
    }
}
