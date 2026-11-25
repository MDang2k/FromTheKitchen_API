using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FromTheKitchen_API.Data;
using FromTheKitchen_API.Models.Dto;
using FromTheKitchen_API.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FromTheKitchen_API.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory: the same Program.cs, middleware, Identity and JWT setup,
/// with SQL Server swapped for a private SQLite in-memory database and a throwaway web root.
/// Every factory instance gets its own empty database (plus the seeded menu).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "test-only-jwt-signing-key-that-is-long-enough-0123456789";
    public const string SeedAdminEmail = "seed-admin@example.test";
    public const string SeedAdminPassword = "Seed!Admin-Test-Pass1";
    public const string FrontendOrigin = "https://frontend.example.test";

    // An in-memory SQLite database lives as long as its connection stays open
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private Task<TestUser>? _admin;

    // Throwaway folder per factory: <root>/wwwroot is the web root, <root>/uploads/images takes uploads
    private readonly string _root = Directory.CreateTempSubdirectory("ftk-tests-").FullName;
    public string WebRoot => Directory.CreateDirectory(Path.Combine(_root, "wwwroot")).FullName;
    public string ImagesPath => Path.Combine(_root, "uploads", "images");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ApiSettings:Secret", JwtSecret);
        builder.UseSetting("SeedAdmin:Email", SeedAdminEmail);
        builder.UseSetting("SeedAdmin:Password", SeedAdminPassword);
        builder.UseSetting("Cors:AllowedOrigins:0", FrontendOrigin);
        builder.UseSetting("ImageStorage:Path", ImagesPath);
        builder.UseWebRoot(WebRoot);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            _connection.Open();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // WebApplicationFactory can call this more than once (Dispose and DisposeAsync)
        if (disposing && Directory.Exists(_root))
        {
            _connection.Dispose();
            Directory.Delete(_root, recursive: true);
        }
    }

    public HttpClient ClientFor(TestUser? user = null)
    {
        var client = CreateClient();
        if (user != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        }
        return client;
    }

    /// <summary>The Admin that the API seeds from configuration at startup.</summary>
    public Task<TestUser> GetAdminAsync() => _admin ??= LoginAsync(SeedAdminEmail, SeedAdminPassword);

    public Task<TestUser> CreateCustomerAsync() => RegisterAndLoginAsync(SD.Role_Customer);

    public static RegisterRequestDTO NewRegistration(string role) => new()
    {
        Email = $"user_{Guid.NewGuid():N}@example.test",
        Password = $"Test!{Guid.NewGuid():N}"[..20] + "Aa1",
        Name = $"Test {role}",
        Role = role,
    };

    public async Task<TestUser> RegisterAndLoginAsync(string role, TestUser? registeredBy = null)
    {
        var registration = NewRegistration(role);
        var registerResponse = await ClientFor(registeredBy).PostAsJsonAsync("/api/Auth/register", registration);
        registerResponse.EnsureSuccessStatusCode();
        return await LoginAsync(registration.Email, registration.Password);
    }

    public async Task<TestUser> LoginAsync(string email, string password)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/Auth/login", new LoginRequestDTO { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var login = (await response.ReadEnvelopeAsync<LoginResponseDTO>()).Result!;
        string id = new JwtSecurityTokenHandler().ReadJwtToken(login.Token).Claims.First(c => c.Type == "id").Value;
        return new TestUser(id, email, login.Token, login.Role);
    }
}

public sealed record TestUser(string Id, string Email, string Token, string Role);
