using FromTheKitchen_API.Models;
using FromTheKitchen_API.Utility;
using Microsoft.AspNetCore.Identity;

namespace FromTheKitchen_API.Data
{
    /// <summary>
    /// Creates the roles and the first Admin at startup, from SeedAdmin:Email / SeedAdmin:Password.
    /// Nobody can register as Admin through the API, so this is how a new database gets one.
    /// Safe to run on every start.
    /// </summary>
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { SD.Role_Admin, SD.Role_Customer })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            string? email = config["SeedAdmin:Email"];
            string? password = config["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email, Name = config["SeedAdmin:Name"] ?? "Admin" };
                ThrowIfFailed(await userManager.CreateAsync(user, password));
            }
            else if (!await userManager.IsInRoleAsync(user, SD.Role_Admin) && !await userManager.CheckPasswordAsync(user, password))
            {
                // Self-registration needs no email confirmation, so an existing account with this email
                // may belong to anyone. Only promote it if the operator knows its password.
                throw new InvalidOperationException(
                    $"Could not seed the Admin account: {email} already exists and SeedAdmin:Password does not match its password.");
            }

            if (!await userManager.IsInRoleAsync(user, SD.Role_Admin))
            {
                ThrowIfFailed(await userManager.AddToRoleAsync(user, SD.Role_Admin));
            }
        }

        // A bad seed config should stop the app loudly, not start it without an Admin
        private static void ThrowIfFailed(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Could not seed the Admin account: "
                    + string.Join(" ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
