using Microsoft.Playwright;
using PlugIn.Automate.Client;
using Tests.E2E.Config;
using Tests.E2E.Fixtures;
using Xunit.Abstractions;

namespace Tests.E2E.Tests
{
    /// <summary>
    /// Example tests that use [UseAccount] for automatic login.
    /// The framework calls LoginAsync before each test body runs.
    /// Add your own test classes following this pattern.
    /// </summary>
    [Collection("Playwright.1")]
    [UseAccount(AutomationAccount.SysAdmin)]
    public class AuthenticatedTests : AppPageTestBase
    {
        public AuthenticatedTests(PlaywrightFixturePool1 fixture, ITestOutputHelper output)
            : base(fixture, output) { }

        [Fact]
        public async Task Dashboard_IsVisible_WhenLoggedIn()
        {
            // LoginAsync has already run by the time this method executes.
            await Page.GotoAsync("/");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Replace with a selector that exists on your application's home/dashboard page
            var heading = Page.GetByRole(AriaRole.Heading, new() { Level = 1 });
            await heading.WaitForAsync();

            Assert.True(await heading.IsVisibleAsync());
        }
    }
}
