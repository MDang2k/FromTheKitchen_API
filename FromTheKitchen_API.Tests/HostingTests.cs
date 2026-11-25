using System.Net;
using FromTheKitchen_API.Tests.Infrastructure;

namespace FromTheKitchen_API.Tests;

public class HostingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_health_endpoint_reports_healthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_frontend_origin_may_call_the_api_from_the_browser()
    {
        var response = await factory.CreateClient().SendAsync(Preflight(ApiFactory.FrontendOrigin));

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowed));
        Assert.Equal(ApiFactory.FrontendOrigin, Assert.Single(allowed));
    }

    [Fact]
    public async Task Other_origins_get_no_cors_permission()
    {
        var response = await factory.CreateClient().SendAsync(Preflight("https://evil.example.test"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // What a browser sends before a cross-origin call that carries an Authorization header
    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/MenuItem");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        return request;
    }
}
