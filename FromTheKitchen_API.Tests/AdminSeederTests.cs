using System.Net;
using System.Net.Http.Json;
using FromTheKitchen_API.Data;
using FromTheKitchen_API.Models;
using FromTheKitchen_API.Tests.Infrastructure;
using FromTheKitchen_API.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FromTheKitchen_API.Tests;

// Each test gets its own database, because they all care about who is Admin
public class AdminSeederTests
{
    [Fact]
    public async Task Seeding_again_on_restart_keeps_a_single_admin()
    {
        using var api = new ApiFactory();
        using var scope = api.Services.CreateScope();

        await AdminSeeder.SeedAsync(scope.ServiceProvider, api.Services.GetRequiredService<IConfiguration>());

        var admins = await UserManager(scope).GetUsersInRoleAsync(SD.Role_Admin);
        Assert.Equal(ApiFactory.SeedAdminEmail, Assert.Single(admins).Email);
    }

    [Fact]
    public async Task An_existing_account_with_the_seed_email_and_password_is_promoted_and_logs_in_as_admin()
    {
        using var api = new ApiFactory();
        var registration = ApiFactory.NewRegistration(SD.Role_Customer);
        (await api.CreateClient().PostAsJsonAsync("/api/Auth/register", registration)).EnsureSuccessStatusCode();
        using var scope = api.Services.CreateScope();

        await AdminSeeder.SeedAsync(scope.ServiceProvider, Config(registration.Email, registration.Password));

        var users = UserManager(scope);
        Assert.Single(users.Users.Where(u => u.Email == registration.Email));
        // The token carries one role; a promoted user must get Admin, not whichever role comes back first
        var login = await api.LoginAsync(registration.Email, registration.Password);
        Assert.Equal(SD.Role_Admin, login.Role);
        Assert.Equal(HttpStatusCode.OK, (await api.ClientFor(login).GetAsync("/api/AuthTest/1")).StatusCode);
    }

    [Fact]
    public async Task Someone_elses_account_is_not_promoted_when_the_seed_password_does_not_match()
    {
        // e.g. SeedAdmin__Email is changed after go-live to an address a stranger already registered
        using var api = new ApiFactory();
        var stranger = await api.CreateCustomerAsync();
        using var scope = api.Services.CreateScope();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AdminSeeder.SeedAsync(scope.ServiceProvider, Config(stranger.Email, "Operator!Chosen-Pass1")));

        Assert.Contains("password", error.Message);
        var users = UserManager(scope);
        Assert.False(await users.IsInRoleAsync((await users.FindByEmailAsync(stranger.Email))!, SD.Role_Admin));
    }

    [Fact]
    public async Task A_seed_password_that_breaks_the_identity_rules_stops_startup()
    {
        using var api = new ApiFactory();
        using var scope = api.Services.CreateScope();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AdminSeeder.SeedAsync(scope.ServiceProvider, Config($"weak_{Guid.NewGuid():N}@example.test", "short")));

        Assert.Contains("Passwords must", error.Message);
    }

    private static UserManager<ApplicationUser> UserManager(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    private static IConfiguration Config(string email, string password) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SeedAdmin:Email"] = email,
            ["SeedAdmin:Password"] = password,
        })
        .Build();
}
