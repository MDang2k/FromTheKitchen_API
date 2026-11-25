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
    public async Task An_existing_customer_with_the_seed_email_is_promoted_not_duplicated()
    {
        using var api = new ApiFactory();
        var customer = await api.CreateCustomerAsync();
        using var scope = api.Services.CreateScope();

        await AdminSeeder.SeedAsync(scope.ServiceProvider, Config(customer.Email, "Unused!Password1"));

        var users = UserManager(scope);
        var user = await users.FindByEmailAsync(customer.Email);
        Assert.True(await users.IsInRoleAsync(user!, SD.Role_Admin));
        Assert.Single(users.Users.Where(u => u.Email == customer.Email));
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
