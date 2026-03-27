using Tests.Api.Fixtures;

namespace Tests.Api.Tests
{
    /// <summary>
    /// Example API integration tests.
    /// Replace the endpoint paths and assertions with calls to your own API.
    /// </summary>
    public class ApiTests : ApiTestBase
    {
        public ApiTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Get_ProtectedEndpoint_WithoutToken_Returns401()
        {
            // Replace with a real protected endpoint from your API
            var response = await Anon.GetAsync("/api/items");

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_ProtectedEndpoint_WithToken_Returns200()
        {
            // Auth client has a valid Bearer token injected by ApiTestBase.InitializeAsync
            var response = await Auth.GetAsync("/api/items");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }
}
