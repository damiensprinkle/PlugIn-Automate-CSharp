namespace Tests.E2E.Fixtures
{
    [CollectionDefinition("Playwright.1")]
    public class PlaywrightCollection1 : ICollectionFixture<PlaywrightFixturePool1> { }

    [CollectionDefinition("Playwright.2")]
    public class PlaywrightCollection2 : ICollectionFixture<PlaywrightFixturePool2> { }
}
