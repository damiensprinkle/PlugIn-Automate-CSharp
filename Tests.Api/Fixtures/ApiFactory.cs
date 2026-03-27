using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Api.Fixtures
{
    /// <summary>
    /// Spins up your API in-process for integration testing.
    ///
    /// -- TO GET STARTED --
    /// 1. Add a ProjectReference to your API project in Tests.Api.csproj.
    /// 2. Replace `YourApi.Program` below with your API's Program class.
    /// 3. Override ConfigureWebHost to inject test config and swap the database.
    ///
    /// Example:
    ///   public class ApiFactory : WebApplicationFactory&lt;YourApi.Program&gt;
    ///   {
    ///       protected override void ConfigureWebHost(IWebHostBuilder builder)
    ///       {
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
    public class ApiFactory : WebApplicationFactory<YourApi.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }
}
