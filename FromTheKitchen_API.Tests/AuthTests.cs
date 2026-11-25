using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FromTheKitchen_API.Models.Dto;
using FromTheKitchen_API.Tests.Infrastructure;
using FromTheKitchen_API.Utility;

namespace FromTheKitchen_API.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_then_login_returns_a_jwt_with_the_customer_role()
    {
        var registration = ApiFactory.NewRegistration(SD.Role_Customer);
        var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/Auth/register", registration);
        var login = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDTO { Email = registration.Email, Password = registration.Password });

        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = (await login.ReadEnvelopeAsync<LoginResponseDTO>()).Result!;
        Assert.Equal(registration.Email, body.Email);
        Assert.Equal(SD.Role_Customer, body.Role);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Token);
        Assert.Contains(jwt.Claims, c => c.Type == "id" && c.Value.Length > 0);
        Assert.Contains(jwt.Claims, c => c.Type is "role" or ClaimTypes.Role && c.Value == SD.Role_Customer);
        Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddDays(6), DateTime.UtcNow.AddDays(8));
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_rejected()
    {
        var registration = ApiFactory.NewRegistration(SD.Role_Customer);
        await factory.CreateClient().PostAsJsonAsync("/api/Auth/register", registration);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/Auth/login",
            new LoginRequestDTO { Email = registration.Email, Password = registration.Password + "x" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid Credentials", (await response.ReadEnvelopeAsync<object>()).ErrorMessages);
    }

    [Fact]
    public async Task Register_with_a_weak_password_returns_the_identity_errors()
    {
        var registration = ApiFactory.NewRegistration(SD.Role_Customer);
        registration.Password = "abc";

        var response = await factory.CreateClient().PostAsJsonAsync("/api/Auth/register", registration);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEmpty((await response.ReadEnvelopeAsync<object>()).ErrorMessages);
    }

    [Fact]
    public async Task Nobody_can_self_register_as_admin_even_on_a_fresh_database()
    {
        // Own factory: nothing has touched this database except startup
        using var freshApi = new ApiFactory();
        var registration = ApiFactory.NewRegistration(SD.Role_Admin);

        var response = await freshApi.CreateClient().PostAsJsonAsync("/api/Auth/register", registration);
        var login = await freshApi.CreateClient().PostAsJsonAsync("/api/Auth/login",
            new LoginRequestDTO { Email = registration.Email, Password = registration.Password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode); // the account was never created
    }

    [Fact]
    public async Task The_admin_from_configuration_can_log_in_with_the_admin_role()
    {
        var admin = await factory.LoginAsync(ApiFactory.SeedAdminEmail, ApiFactory.SeedAdminPassword);

        Assert.Equal(SD.Role_Admin, admin.Role);
    }

    [Fact]
    public async Task A_customer_cannot_create_an_admin()
    {
        await factory.GetAdminAsync();
        var customer = await factory.CreateCustomerAsync();

        var response = await factory.ClientFor(customer).PostAsJsonAsync("/api/Auth/register", ApiFactory.NewRegistration("admin"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Only admins can create admin accounts", (await response.ReadEnvelopeAsync<object>()).ErrorMessages);
    }

    [Fact]
    public async Task An_admin_can_create_another_admin()
    {
        var admin = await factory.GetAdminAsync();

        var newAdmin = await factory.RegisterAndLoginAsync(SD.Role_Admin, registeredBy: admin);

        Assert.Equal(SD.Role_Admin, newAdmin.Role);
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        var customer = await factory.CreateCustomerAsync();
        var forged = customer with { Token = customer.Token[..^4] + "AAAA" };

        var response = await factory.ClientFor(forged).GetAsync("/api/OrderHeader");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
