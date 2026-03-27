using Microsoft.Playwright;
using PlugIn.Automate.Client;
using Tests.E2E.Config;
using Tests.E2E.Fixtures;
using Tests.E2E.Visual;
using Xunit.Abstractions;

namespace Tests.E2E.Tests.Visual
{
    /// <summary>
    /// Visual regression tests.
    ///
    /// First run: tests fail with instructions on how to create baselines.
    /// To create baselines:
    ///   UPDATE_VISUAL_BASELINES=true dotnet test Tests.E2E --filter "FullyQualifiedName~Visual"
    ///   Then copy Tests.E2E/bin/.../Baselines/*.png into Tests.E2E/Visual/Baselines/ and commit.
    /// </summary>
    [Collection("Playwright.1")]
    [UseAccount(AutomationAccount.SysAdmin)]
    public class VisualTests : AppPageTestBase, IClassFixture<VisualTestContext>
    {
        private readonly VisualTestContext _visual;

        public VisualTests(PlaywrightFixturePool1 fixture, ITestOutputHelper output, VisualTestContext visual)
            : base(fixture, output)
        {
            _visual = visual;
        }

        [Fact]
        public async Task LoginPage_MatchesBaseline()
        {
            await Page.GotoAsync("/login");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var screenshot = await Page.ScreenshotAsync(new() { FullPage = true });
            var result     = _visual.Comparer.Compare("login-page", screenshot);

            _visual.AddResult(result);

            Assert.True(result.Status != VisualTestStatus.Failed,
                result.Message ?? $"Visual diff {result.DiffFraction:P3} exceeds threshold.");
        }

        [Fact]
        public async Task HomePage_MatchesBaseline()
        {
            // LoginAsync has already run so the user is authenticated
            await Page.GotoAsync("/");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var screenshot = await Page.ScreenshotAsync(new() { FullPage = true });
            var result     = _visual.Comparer.Compare("home-page", screenshot);

            _visual.AddResult(result);

            Assert.True(result.Status != VisualTestStatus.Failed,
                result.Message ?? $"Visual diff {result.DiffFraction:P3} exceeds threshold.");
        }
    }
}
