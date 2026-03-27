using PlugIn.Automate.Client;
using Tests.Api.Live.Fixtures;
using Xunit.Abstractions;

namespace Tests.Api.Live.Tests
{
    /// <summary>
    /// Example live API tests.
    /// Replace the endpoint paths and assertions with calls to your own API.
    /// </summary>
    [Collection("LiveApi.1")]
    [UseAccount(AutomationAccount.SysAdmin)]
    public class LiveApiTests : AppLiveApiTestBase
    {
        public LiveApiTests(LiveApiFixturePool1 fixture, ITestOutputHelper output)
            : base(fixture, output) { }

        [Fact]
        public async Task Get_ProtectedEndpoint_WithToken_Returns200()
        {
            // Auth has a valid Bearer token injected before this test runs
            var response = await Auth.GetAsync("/api/items");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_ProtectedEndpoint_WithoutToken_Returns401()
        {
            var response = await Anon.GetAsync("/api/items");

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
