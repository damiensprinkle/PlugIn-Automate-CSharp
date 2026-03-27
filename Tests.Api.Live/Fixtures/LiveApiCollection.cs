namespace Tests.Api.Live.Fixtures
{
    [CollectionDefinition("LiveApi.1")]
    public class LiveApiCollection1 : ICollectionFixture<LiveApiFixturePool1> { }

    [CollectionDefinition("LiveApi.2")]
    public class LiveApiCollection2 : ICollectionFixture<LiveApiFixturePool2> { }
}
