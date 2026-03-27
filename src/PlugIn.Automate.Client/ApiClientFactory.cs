using System.Net.Http.Headers;

namespace PlugIn.Automate.Client
{
    /// <summary>
    /// Creates <see cref="HttpClient"/> instances pre-configured for use with a
    /// NSwag-generated typed client.
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <see cref="CreateAnonymous"/> / <see cref="CreateAuthenticated"/> --
    ///     for live servers (E2E tests and manual API calls).
    ///   </item>
    ///   <item>
    ///     <see cref="CreateForFactory"/> -- for in-process <c>HttpClient</c> instances
    ///     provided by <c>WebApplicationFactory</c> (API integration tests).
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// Wrap the returned <see cref="HttpClient"/> with your NSwag-generated client, e.g.:
    /// <code>
    /// var http   = ApiClientFactory.CreateAuthenticated(baseUrl, token);
    /// var client = new MyApiClient(baseUrl, http);
    /// </code>
    /// </para>
    /// </summary>
    public static class ApiClientFactory
    {
        /// <summary>Creates an anonymous <see cref="HttpClient"/> for a live server.</summary>
        public static HttpClient CreateAnonymous(string baseUrl)
        {
            var http = new HttpClient();
            http.BaseAddress = new Uri(Normalize(baseUrl));
            return http;
        }

        /// <summary>Creates a Bearer-authenticated <see cref="HttpClient"/> for a live server.</summary>
        public static HttpClient CreateAuthenticated(string baseUrl, string token)
        {
            var http = new HttpClient();
            http.BaseAddress = new Uri(Normalize(baseUrl));
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return http;
        }

        /// <summary>
        /// Attaches a Bearer token to an existing <see cref="HttpClient"/>.
        /// Use with <c>WebApplicationFactory.CreateClient()</c> in API integration tests
        /// where the factory manages the client lifetime.
        /// </summary>
        public static HttpClient CreateForFactory(HttpClient httpClient, string token)
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return httpClient;
        }

        private static string Normalize(string url)
        {
            return url.EndsWith('/') ? url : url + "/";
        }
    }
}
