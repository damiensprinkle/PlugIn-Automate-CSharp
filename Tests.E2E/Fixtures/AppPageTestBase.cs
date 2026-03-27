using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using PlugIn.Automate.Client;
using Tests.E2E.Config;
using Xunit.Abstractions;

namespace Tests.E2E.Fixtures
{
    /// <summary>
    /// Concrete base class for this application's Playwright tests.
    ///
    /// -- CONFIGURE THIS FILE --
    /// Implement LoginAsync below to match your application's auth flow.
    /// The default implementation shows the JWT + localStorage pattern used by
    /// most React/Vue/Angular SPAs backed by an ASP.NET Core API.
    ///
    /// If your app uses session cookies, OAuth redirects, or a different token
    /// storage mechanism, adapt the method accordingly.
    /// </summary>
    public abstract class AppPageTestBase : PageTestBase
    {
        protected AppPageTestBase(PlaywrightFixture fixture, ITestOutputHelper output)
            : base(fixture, output) { }

        /// <summary>
        /// Authenticates the given account and makes the browser session appear logged in.
        ///
        /// Default implementation (JWT + localStorage):
        ///   1. POST credentials to your login endpoint.
        ///   2. Extract the JWT from the JSON response.
        ///   3. Inject it into localStorage so the SPA treats the session as authenticated.
        ///
        /// Adapt the endpoint path, request shape, JSON property name, and localStorage key
        /// to match your application.
        /// </summary>
        protected override async Task LoginAsync(TestUserSettings account)
        {
            // Step 1 -- obtain a JWT from the API
            using var http = new HttpClient();

            var payload = JsonSerializer.Serialize(new
            {
                email    = account.Email,
                password = account.Password,
            });

            var response = await http.PostAsync(
                $"{Settings.ApiUrl}/account/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var body  = await response.Content.ReadAsStringAsync();
            var doc   = JsonDocument.Parse(body);

            // Adapt the property name to match your API's response shape.
            // Common alternatives: "access_token", "accessToken", "jwt"
            var token = doc.RootElement.GetProperty("token").GetString()
                ?? throw new InvalidOperationException(
                    "Login response did not contain a 'token' property. " +
                    "Update LoginAsync in AppPageTestBase.cs to match your API.");

            // Step 2 -- inject the token into the browser
            await Page.GotoAsync("/");
            await Page.EvaluateAsync("t => localStorage.setItem('jwt', t)", token);
            await Page.ReloadAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        /// <summary>
        /// Registers the account via the API without touching the browser session.
        /// Call this in auth UI tests before navigating to the login page to ensure
        /// the account already exists in the system.
        ///
        /// Adapt the endpoint path and request body shape to match your API.
        /// </summary>
        protected override async Task EnsureAccountExistsAsync(AutomationAccount automationAccount)
        {
            var settings = GetAccount(automationAccount);
            using var http = new HttpClient();

            var payload = JsonSerializer.Serialize(new
            {
                email       = settings.Email,
                password    = settings.Password,
                displayName = settings.DisplayName,
                username    = settings.Username,
            });

            try
            {
                await http.PostAsync(
                    $"{Settings.ApiUrl}/account/register",
                    new StringContent(payload, Encoding.UTF8, "application/json"));
            }
            catch
            {
                // Account may already exist -- ignore errors.
            }
        }
    }
}
