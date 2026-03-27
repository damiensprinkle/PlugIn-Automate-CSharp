using PlugIn.Automate.Client;
using Tests.E2E.Config;
using Tests.E2E.Fixtures;
using Tests.E2E.Pages;
using Xunit.Abstractions;

namespace Tests.E2E.Tests
{
    /// <summary>
    /// Example auth tests.
    /// Uses Pool2 so it can run in parallel with authenticated tests in Pool1.
    /// </summary>
    [Collection("Playwright.2")]
    public class LoginTests : AppPageTestBase
    {
        private readonly LoginPage _loginPage;

        public LoginTests(PlaywrightFixturePool2 fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _loginPage = new LoginPage(Page);
        }

        [Fact]
        public async Task Login_WithValidCredentials_RedirectsToDashboard()
        {
            // Ensure the account exists before driving the login UI
            await EnsureAccountExistsAsync(AutomationAccount.SysAdmin);

            var account = GetAccount(AutomationAccount.SysAdmin);
            await _loginPage.NavigateAsync();
            await _loginPage.LoginAsync(account.Email, account.Password);

            // Replace with an assertion that matches your post-login page
            Assert.False(Page.Url.Contains("/login"), "Expected to leave the login page after successful login.");
        }
    }
}
