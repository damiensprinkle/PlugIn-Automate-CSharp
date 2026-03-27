using System.Collections.Concurrent;

namespace PlugIn.Automate.Client.Mothers
{
    /// <summary>
    /// Base class for Object Mothers that create test data by calling the real API.
    ///
    /// Results are cached by key so the same logical object (e.g. "the default activity")
    /// is only created once per test run, regardless of how many tests request it.
    /// Use <see cref="CreateOnceAsync"/> when you need an independent, non-cached copy.
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// public class ActivityMother : MotherBase&lt;ActivityDto&gt;
    /// {
    ///     private readonly IMyApiClient _client;
    ///
    ///     public ActivityMother(IMyApiClient client) { _client = client; }
    ///
    ///     // Cached -- returns the same ActivityDto on every call within a test run
    ///     public Task&lt;ActivityDto&gt; DefaultAsync()
    ///         =&gt; GetOrCreateAsync("default", () =&gt;
    ///                _client.CreateActivityAsync(new ActivityFormDtoBuilder().Build()));
    ///
    ///     // Non-cached -- each call creates a fresh activity via the API
    ///     public Task&lt;ActivityDto&gt; CreateOnceAsync(ActivityFormDto dto)
    ///         =&gt; base.CreateOnceAsync(() =&gt; _client.CreateActivityAsync(dto));
    /// }
    /// </code>
    /// </summary>
    public abstract class MotherBase<TDto>
    {
        private readonly ConcurrentDictionary<string, TDto> _cache = new();

        /// <summary>
        /// Returns the cached object for <paramref name="key"/> if it exists, otherwise
        /// calls <paramref name="factory"/> to create it via the API, caches the result, and returns it.
        /// </summary>
        protected async Task<TDto> GetOrCreateAsync(string key, Func<Task<TDto>> factory)
        {
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var item = await factory();
            _cache.TryAdd(key, item);
            return item;
        }

        /// <summary>
        /// Creates an object via <paramref name="factory"/> without caching the result.
        /// Use when each test needs its own independent copy of the object.
        /// </summary>
        protected Task<TDto> CreateOnceAsync(Func<Task<TDto>> factory)
        {
            return factory();
        }
    }
}
