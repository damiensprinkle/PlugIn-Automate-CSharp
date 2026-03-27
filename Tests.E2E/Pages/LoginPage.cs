using Microsoft.Playwright;

namespace Tests.E2E.Pages
{
    /// <summary>
    /// Example page object for a login page.
    /// Replace selectors and the URL with those of your application.
    /// Add more page objects under this folder for each page you want to test.
    /// </summary>
    public class LoginPage
    {
        private readonly IPage _page;

        public LoginPage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateAsync()
        {
            await _page.GotoAsync("/login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        public async Task LoginAsync(string email, string password)
        {
            await _page.GetByLabel("Email").FillAsync(email);
            await _page.GetByLabel("Password").FillAsync(password);
            await _page.Locator("button[type='submit']").ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
