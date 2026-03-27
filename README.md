# PlugIn-Automate-CSharp

A plug-and-play automation starter for .NET 8 projects. Clone this repo, point it at your application, and start writing tests — no scaffolding from scratch.

---

## What's Included

| Project | Purpose |
|---|---|
| `Tests.E2E` | Playwright browser tests — parallel pools, auto-login, tracing, visual regression |
| `Tests.Api` | In-process API tests via `WebApplicationFactory` — no running server needed |
| `Tests.Api.Live` | API tests against a live server — parallel pools, `[UseAccount]`, same pattern as E2E |
| `src/PlugIn.Automate.Client` | Shared client factory, builders, object mothers, `AutomationAccount`, NSwag codegen |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Node.js 18+](https://nodejs.org/) — required by Playwright
- [NSwag CLI](https://github.com/RicoSuter/NSwag) *(optional — only needed for API client generation)*

```bash
dotnet tool install -g NSwag.ConsoleCore
```

---

## Getting Started

### Step 0 — Decide which test projects you need

| Project | When to use |
|---|---|
| `Tests.E2E` | You want browser-level tests (Playwright) |
| `Tests.Api` | You want fast in-process API tests (no running server) |
| `Tests.Api.Live` | You want API tests against a live running server |

Remove any projects you don't need. For example, to drop the live API tests:

```bash
dotnet sln remove Tests.Api.Live
rm -rf Tests.Api.Live
```

Run the compatibility script at any time to verify your environment and catch missing configuration:

```powershell
.\Check-Compatibility.ps1
```

When run interactively the script will also print the exact removal commands for whichever project you choose not to use.

---

### Step 1 — Configure credentials and URLs

**E2E tests** — edit `Tests.E2E/testsettings.json`:

```json
{
  "BaseUrl": "http://localhost:3000",
  "ApiUrl":  "http://localhost:5000/api",
  "Headless": true,
  "Pools": {
    "Pool1": {
      "SysAdmin":     { "Email": "admin@example.com",  "Password": "..." },
      "StandardUser": { "Email": "user@example.com",   "Password": "..." }
    },
    "Pool2": {
      "SysAdmin":     { "Email": "admin2@example.com", "Password": "..." },
      "StandardUser": { "Email": "user2@example.com",  "Password": "..." }
    }
  }
}
```

**Live API tests** — edit `Tests.Api.Live/apitestsettings.json` (same pool structure, no browser settings):

```json
{
  "BaseUrl": "http://localhost:5000",
  "Pools": {
    "Pool1": { "SysAdmin": { ... }, "StandardUser": { ... } },
    "Pool2": { "SysAdmin": { ... }, "StandardUser": { ... } }
  }
}
```

Local overrides (never committed) go in `testsettings.local.json` / `apitestsettings.local.json`. All values can also be set via environment variables — see [Environment Variables](#environment-variables).

---

### Step 2 — Implement login for E2E tests

Open `Tests.E2E/Fixtures/AppPageTestBase.cs` and adapt `LoginAsync`. The default posts to a JWT login endpoint and injects the token into `localStorage`:

```csharp
protected override async Task LoginAsync(TestUserSettings user)
{
    using var http = new HttpClient();
    var payload = JsonSerializer.Serialize(new { email = user.Email, password = user.Password });
    var response = await http.PostAsync($"{Settings.ApiUrl}/account/login",
        new StringContent(payload, Encoding.UTF8, "application/json"));
    response.EnsureSuccessStatusCode();

    var token = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
        .RootElement.GetProperty("token").GetString()!;

    await Page.GotoAsync("/");
    await Page.EvaluateAsync("t => localStorage.setItem('jwt', t)", token);
    await Page.ReloadAsync();
}
```

---

### Step 3 — Implement login for live API tests

Open `Tests.Api.Live/Fixtures/AppLiveApiTestBase.cs` and adapt `LoginAsync`. It returns the bearer token string rather than driving a browser:

```csharp
protected override async Task<string> LoginAsync(TestUserSettings user)
{
    using var client = ApiClientFactory.CreateAnonymous(Settings.BaseUrl);
    var body = JsonSerializer.Serialize(new { email = user.Email, password = user.Password });
    var response = await client.PostAsync("/api/account/login",
        new StringContent(body, Encoding.UTF8, "application/json"));
    response.EnsureSuccessStatusCode();

    return JsonDocument.Parse(await response.Content.ReadAsStringAsync())
        .RootElement.GetProperty("token").GetString()!;
}
```

---

### Step 4 — Wire up the in-process API factory

`Tests.Api` runs your API in-process using `WebApplicationFactory`. Three things need to be wired up:

**4a — Add a project reference** in `Tests.Api/Tests.Api.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\path\to\YourApi\YourApi.csproj" />
</ItemGroup>
```

**4b — Set the entry point** in `Tests.Api/Fixtures/ApiFactory.cs` — replace `YourApi.Program` with your API's `Program` class:

```csharp
public class ApiFactory : WebApplicationFactory<YourApi.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
        });
    }
}
```

**4c — Adapt the auth setup** in `Tests.Api/Fixtures/ApiTestBase.cs`. Update the test credentials and adapt the register/login endpoint paths and response shape to match your API:

```csharp
protected const string TestEmail    = "test@example.com";
protected const string TestPassword = "Pa$$w0rd";
```

The `InitializeAsync` method registers and logs in the test user before each test, then injects the token into the `Auth` client. Adapt the endpoint paths (`/api/account/register`, `/api/account/login`) and the JSON property name (`"token"`) to match your API.

---

## Running the Tests

```bash
# Build everything
dotnet build

# E2E tests
cd Tests.E2E
dotnet playwright install chromium
dotnet test

# In-process API tests
dotnet test Tests.Api

# Live API tests (server must be running)
dotnet test Tests.Api.Live
```

---

## Project Structure

```
PlugIn-Automate-CSharp/
├── src/
│   └── PlugIn.Automate.Client/
│       ├── AutomationAccount.cs      # Shared account role enum (SysAdmin, StandardUser)
│       ├── UseAccountAttribute.cs    # [UseAccount] attribute — shared by E2E and Live API
│       ├── TestUserSettings.cs       # Credential POCO used by all pool-based projects
│       ├── ApiClientFactory.cs       # Creates anonymous and authenticated HttpClients
│       ├── Builders/BuilderBase.cs   # Fluent DTO builder base class
│       ├── Mothers/MotherBase.cs     # Cached test-data object creation
│       ├── nswag.json                # NSwag codegen config
│       └── swagger.json              # Placeholder — replace with your API's spec
├── Tests.E2E/
│   ├── Config/
│   │   └── TestSettings.cs           # E2E-specific settings (BaseUrl, ApiUrl, Headless, Pools)
│   ├── Fixtures/
│   │   ├── PlaywrightFixture.cs      # Browser lifecycle, Pool1 + Pool2 subclasses
│   │   ├── PlaywrightCollection.cs   # [CollectionDefinition] for Playwright.1 and Playwright.2
│   │   ├── PageTestBase.cs           # Framework base — context/page lifecycle, tracing, [UseAccount]
│   │   └── AppPageTestBase.cs        # User-editable — implement LoginAsync and EnsureAccountExistsAsync
│   ├── Pages/
│   │   └── LoginPage.cs              # Example page object
│   ├── Tests/
│   │   ├── AuthenticatedTests.cs     # Example tests using [UseAccount]
│   │   ├── LoginTests.cs             # Example auth UI tests
│   │   └── Visual/VisualTests.cs     # Visual regression tests
│   ├── Visual/
│   │   ├── Baselines/                # Committed baseline screenshots
│   │   ├── VisualComparer.cs         # Pixel diff via Magick.NET
│   │   ├── VisualTestContext.cs      # Class fixture — collects results, generates report
│   │   ├── VisualTestResult.cs       # Result record (Passed, Failed, NewBaseline)
│   │   └── VisualReport.cs           # Self-contained HTML report with base64 images
│   └── testsettings.json
├── Tests.Api/
│   ├── Fixtures/
│   │   ├── ApiFactory.cs             # WebApplicationFactory — user adds project ref + DB swap
│   │   ├── ApiCollection.cs          # [CollectionDefinition("Api")]
│   │   └── ApiTestBase.cs            # Provides Anon + Auth HttpClients via in-process factory
│   └── Tests/
│       └── ApiTests.cs               # Example in-process API tests
├── Tests.Api.Live/
│   ├── Config/
│   │   └── LiveApiSettings.cs        # Live API settings (BaseUrl + Pools)
│   ├── Fixtures/
│   │   ├── LiveApiFixture.cs         # Settings loader, Pool1 + Pool2 subclasses
│   │   ├── LiveApiCollection.cs      # [CollectionDefinition] for LiveApi.1 and LiveApi.2
│   │   ├── LiveApiTestBase.cs        # Framework base — [UseAccount] resolution, Anon + Auth
│   │   └── AppLiveApiTestBase.cs     # User-editable — implement LoginAsync
│   ├── Tests/
│   │   └── LiveApiTests.cs           # Example live API tests
│   └── apitestsettings.json
├── Check-Compatibility.ps1
├── .editorconfig
└── .gitignore
```

---

## E2E Tests

### Parallel Execution

Tests run across two xUnit collections (`Playwright.1` / `Playwright.2`), each with its own browser process and credential pool. Test classes in different collections run in parallel without sharing browser state or accounts.

```csharp
[Collection("Playwright.1")]
[UseAccount(AutomationAccount.SysAdmin)]
public class DashboardTests : AppPageTestBase
{
    public DashboardTests(PlaywrightFixturePool1 fixture, ITestOutputHelper output)
        : base(fixture, output) { }
}
```

Use `Pool2` / `PlaywrightFixturePool2` / `Playwright.2` for a second parallel group (e.g., auth UI tests that need their own credentials).

### `[UseAccount]`

Apply at the class or method level. The framework resolves the account from `testsettings.json`, calls `LoginAsync`, and completes authentication before the test body runs.

- Class-level: every test in the class logs in as that account
- Method-level: overrides the class-level attribute for that specific test
- No attribute: no auto-login — use for tests that drive the login UI themselves

### Tracing

A Playwright trace is recorded for every test and saved to `traces/<TestName>.zip`. Open a trace with:

```bash
dotnet playwright show-trace traces/MyTest.zip
```

### Page Objects

Add page objects to `Tests.E2E/Pages/`:

```csharp
public class ItemsPage(IPage page)
{
    public Task NavigateAsync() => page.GotoAsync("/items");
    public ILocator ItemRows => page.Locator("table tbody tr");
}
```

---

## In-Process API Tests (`Tests.Api`)

`ApiTestBase` starts your API in-process and provides two `HttpClient` instances per test:

| Client | Description |
|---|---|
| `Anon` | No Authorization header |
| `Auth` | Bearer token obtained by logging in before each test |

```csharp
public class ItemApiTests : ApiTestBase
{
    public ItemApiTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateItem_Returns201()
    {
        var body = JsonContent.Create(new { name = "Widget", price = 9.99 });
        var response = await Auth.PostAsync("/api/items", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

Adapt the hardcoded credentials and endpoint paths at the top of `ApiTestBase.cs` to match your API.

---

## Live API Tests (`Tests.Api.Live`)

`Tests.Api.Live` targets a running server. It mirrors the E2E architecture: parallel credential pools, `[UseAccount]`, and auto-login via `AppLiveApiTestBase`.

### Writing Tests

```csharp
[Collection("LiveApi.1")]
[UseAccount(AutomationAccount.SysAdmin)]
public class ItemLiveTests : AppLiveApiTestBase
{
    public ItemLiveTests(LiveApiFixturePool1 fixture, ITestOutputHelper output)
        : base(fixture, output) { }

    [Fact]
    public async Task GetItems_Returns200()
    {
        var response = await Auth.GetAsync("/api/items");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetItems_Unauthenticated_Returns401()
    {
        var response = await Anon.GetAsync("/api/items");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

`Auth` and `Anon` are plain `HttpClient` instances with `BaseAddress` set to `apitestsettings.json > BaseUrl`. `LoginAsync` in `AppLiveApiTestBase.cs` runs before every test decorated with `[UseAccount]`.

Use `[Collection("LiveApi.2")]` + `LiveApiFixturePool2` for a second parallel group.

---

## `PlugIn.Automate.Client`

### Shared Account Types

`AutomationAccount`, `UseAccountAttribute`, and `TestUserSettings` live here so they are shared across `Tests.E2E` and `Tests.Api.Live` without duplication.

```csharp
// Add roles to match your application
public enum AutomationAccount { SysAdmin, StandardUser }
```

### `ApiClientFactory`

Creates `HttpClient` instances:

```csharp
// Anonymous — no Authorization header
var anon = ApiClientFactory.CreateAnonymous("http://localhost:5000");

// Authenticated — Bearer token set on DefaultRequestHeaders
var auth = ApiClientFactory.CreateAuthenticated("http://localhost:5000", token);

// For WebApplicationFactory (Tests.Api)
var auth = ApiClientFactory.CreateForFactory(factory.CreateClient(), token);
```

### Builders

Fluent test-data builders. Override `Defaults()` and use `.Set()` to override individual fields:

```csharp
public class CreateItemBuilder : BuilderBase<CreateItemDto, CreateItemBuilder>
{
    protected override CreateItemDto Defaults() => new() { Name = "Item", Price = 9.99m };
}

var dto = new CreateItemBuilder().Set(x => x.Name, "Widget").Build();
```

### Object Mothers

Cached API-created objects for test data setup:

```csharp
public class ItemMother : MotherBase<ItemDto>
{
    private readonly IItemsClient _client;

    // Returns the same object for every call with "default" — creates only once
    public Task<ItemDto> DefaultAsync() =>
        GetOrCreateAsync("default", () =>
            _client.CreateItemAsync(new CreateItemBuilder().Build()));

    // Always creates a fresh object
    public Task<ItemDto> FreshAsync() =>
        CreateOnceAsync(() => _client.CreateItemAsync(new CreateItemBuilder().Build()));
}
```

### NSwag Client Generation

Replace `src/PlugIn.Automate.Client/swagger.json` with your API's OpenAPI spec, then build. NSwag runs as a pre-build step and outputs `Client/ApiClient.g.cs`. If NSwag CLI is not installed the step is silently skipped.

---

## Visual Regression Tests

### Creating Baselines

```bash
UPDATE_VISUAL_BASELINES=true dotnet test Tests.E2E --filter "FullyQualifiedName~Visual"
```

Baselines are written to `Tests.E2E/Visual/Baselines/` — commit them.

### Running

```bash
dotnet test Tests.E2E --filter "FullyQualifiedName~Visual"
```

Tests fail if a screenshot differs from its baseline by more than 0.1%. A self-contained HTML report with baseline, actual, and diff images is written to `Tests.E2E/VisualResults/`.

### Adding a Visual Test

```csharp
[Collection("Playwright.1")]
[UseAccount(AutomationAccount.SysAdmin)]
public class MyVisualTests : AppPageTestBase, IClassFixture<VisualTestContext>
{
    private readonly VisualTestContext _visual;

    public MyVisualTests(PlaywrightFixturePool1 fixture, ITestOutputHelper output, VisualTestContext visual)
        : base(fixture, output) { _visual = visual; }

    [Fact]
    public async Task ItemsPage_MatchesBaseline()
    {
        await Page.GotoAsync("/items");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var screenshot = await Page.ScreenshotAsync(new() { FullPage = true });
        var result = _visual.Comparer.Compare("items-page", screenshot);
        _visual.AddResult(result);

        Assert.True(result.Status != VisualTestStatus.Failed,
            result.Message ?? $"Visual diff {result.DiffFraction:P3} exceeds threshold.");
    }
}
```

---

## Compatibility Check

Before running tests, verify your environment and configuration:

```powershell
.\Check-Compatibility.ps1
```

| Option | Description |
|---|---|
| `-TargetPath <path>` | Check a different directory (default: current) |
| `-ApiUrl <url>` | Probe a live API endpoint as part of the check |
| `-Detailed` | Show extra detail for each check |

When run interactively (not in CI), the script asks whether you intend to use `Tests.Api` (in-process), `Tests.Api.Live` (live server), or both — and prints the exact `dotnet sln remove` commands for whichever project you don't need. This prompt is automatically skipped when `CI`, `GITHUB_ACTIONS`, or `TF_BUILD` environment variables are set.

Checks include: .NET 8 SDK, Node.js 18+, NSwag CLI, Playwright Chromium, solution structure, `.editorconfig`, `.gitignore` completeness, NuGet package versions, Swagger/NSwag config, `testsettings.json` and `apitestsettings.json` pool completeness, and visual baseline counts.

---

## CI/CD

A GitHub Actions workflow at `.github/workflows/ci.yml` builds the solution, runs in-process API tests, installs Playwright, and runs E2E tests. Commit your visual baselines before running CI — do not set `UPDATE_VISUAL_BASELINES` in the workflow environment.

---

## Environment Variables

| Variable | Project | Description |
|---|---|---|
| `E2E_BaseUrl` | `Tests.E2E` | Frontend URL (overrides `testsettings.json`) |
| `E2E_ApiUrl` | `Tests.E2E` | Backend API URL (overrides `testsettings.json`) |
| `E2E_Headless` | `Tests.E2E` | `true` or `false` |
| `UPDATE_VISUAL_BASELINES` | `Tests.E2E` | Set to `true` to create or update baseline screenshots |
| `API_BaseUrl` | `Tests.Api.Live` | API server URL (overrides `apitestsettings.json`) |
