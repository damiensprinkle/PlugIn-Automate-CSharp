using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Api.Fixtures
{
    /// <summary>
    /// Spins up TestApp in-process for integration testing.
    ///
    /// -- TO GET STARTED WITH YOUR OWN API --
    /// 1. Replace the ProjectReference to TestApp in Tests.Api.csproj with a reference
    ///    to your own API project.
    /// 2. Replace `global::Program` below with your API's Program class.
    /// 3. Override ConfigureWebHost to inject test config and swap the database.
    ///
    /// Example:
    ///   public class ApiFactory : WebApplicationFactory&lt;YourApi.Program&gt;
    ///   {
    ///       protected override void ConfigureWebHost(IWebHostBuilder builder)
    ///       {
    ///           builder.UseSetting("TokenKey", "super-secret-test-key-at-least-32-bytes!");
    ///           builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
    ///
    ///           builder.ConfigureServices(services =>
    ///           {
    ///               var descriptor = services.SingleOrDefault(
    ///                   d => d.ServiceType == typeof(DbContextOptions&lt;AppDbContext&gt;));
    ///               if (descriptor != null) services.Remove(descriptor);
    ///
    ///               services.AddDbContext&lt;AppDbContext&gt;(opts =>
    ///                   opts.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
    ///           });
    ///       }
    ///   }
    /// </summary>
    public class ApiFactory : WebApplicationFactory<global::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            // Inject the JWT signing key that TestApp reads from configuration
            builder.UseSetting("TokenKey", "test-signing-key-that-is-at-least-32-bytes!!");
        }
    }
}
