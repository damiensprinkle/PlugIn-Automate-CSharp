using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var tokenKey  = builder.Configuration["TokenKey"] ?? "default-test-key-at-least-32-bytes!!";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = signingKey,
            ValidateIssuer           = false,
            ValidateAudience         = false,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// In-memory stores (reset each time the server starts)
// ---------------------------------------------------------------------------

var users = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // email -> password
var items = new List<object>
{
    new { id = 1, name = "Widget", price = 9.99  },
    new { id = 2, name = "Gadget", price = 19.99 },
};

// ---------------------------------------------------------------------------
// Endpoints
// ---------------------------------------------------------------------------

// POST /api/account/register
app.MapPost("/api/account/register", (RegisterRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email and password are required." });

    if (!users.TryAdd(req.Email, req.Password))
        return Results.Conflict(new { error = "An account with that email already exists." });

    return Results.Ok();
});

// POST /api/account/login
app.MapPost("/api/account/login", (LoginRequest req) =>
{
    if (!users.TryGetValue(req.Email ?? "", out var stored) || stored != req.Password)
        return Results.Unauthorized();

    var claims = new[] { new Claim(ClaimTypes.Email, req.Email!) };
    var creds  = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var jwt    = new JwtSecurityToken(
        claims:            claims,
        expires:           DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(jwt) });
});

// GET /api/items  (requires a valid bearer token)
app.MapGet("/api/items", () => Results.Ok(items))
   .RequireAuthorization();

app.Run();

// ---------------------------------------------------------------------------
// Request records
// ---------------------------------------------------------------------------

public record RegisterRequest(string? Email, string? Password, string? Username, string? DisplayName);
public record LoginRequest(string? Email, string? Password);

// Makes Program accessible to WebApplicationFactory in Tests.Api
public partial class Program { }
