using System.Text;
using System.Text.Json;
using PlugIn.Automate.Client;
using Xunit.Abstractions;

namespace Tests.Api.Live.Fixtures
{
    /// <summary>
    /// Concrete base class for this application's live API tests.
    ///
    /// -- CONFIGURE THIS FILE --
    /// Override LoginAsync to match your API's authentication flow.
    /// The default implementation posts credentials to /api/account/login and
    /// extracts a "token" property from the JSON response -- the same shape used
    /// by most ASP.NET Core JWT APIs. Adapt the endpoint path and property name
    /// if your API differs.
    /// </summary>
    public abstract class AppLiveApiTestBase : LiveApiTestBase
    {
        protected AppLiveApiTestBase(LiveApiFixture fixture, ITestOutputHelper output)
            : base(fixture, output) { }

        /// <summary>
        /// Authenticates the account and returns the bearer token.
        ///
        /// Default: POST to /api/account/login, extract "token" from the response.
        /// Adapt the endpoint path, request body shape, and JSON property name
        /// to match your API.
        /// </summary>
        protected override async Task<string> LoginAsync(TestUserSettings user)
        {
            using var client = ApiClientFactory.CreateAnonymous(Settings.BaseUrl);

            var body = JsonSerializer.Serialize(new
            {
                email    = user.Email,
                password = user.Password,
            });

            var response = await client.PostAsync(
                "/api/account/login",
                new StringContent(body, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc  = JsonDocument.Parse(json);

            // Adapt the property name to match your API's response shape.
            // Common alternatives: "access_token", "accessToken", "jwt"
            return doc.RootElement.GetProperty("token").GetString()
                ?? throw new InvalidOperationException(
                    "Login response did not contain a 'token' property. " +
                    "Update LoginAsync in AppLiveApiTestBase.cs to match your API.");
        }
    }
}
