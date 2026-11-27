using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FromTheKitchen_API.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace FromTheKitchen_API.Tests;

// docs/api/openapi.json is generated from the running app. This test fails when the code and the
// committed file disagree. To refresh the file after an API change, run:
//   UPDATE_OPENAPI=1 dotnet test --filter OpenApiDocumentTests     (PowerShell: $env:UPDATE_OPENAPI=1; dotnet test ...)
public class OpenApiDocumentTests
{
    private static readonly string SpecPath = Path.Combine(RepoRoot(), "docs", "api", "openapi.json");

    [Fact]
    public async Task The_committed_openapi_spec_matches_the_api()
    {
        // The document is only mapped in Development, the same as when you run the app locally
        using var api = new ApiFactory();
        using var dev = api.WithWebHostBuilder(b => b.UseEnvironment("Development"));

        var response = await dev.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string generated = Normalize(await response.Content.ReadAsStringAsync());

        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpecPath)!);
            await File.WriteAllTextAsync(SpecPath, generated);
            return;
        }

        Assert.True(File.Exists(SpecPath), $"{SpecPath} is missing. Generate it with UPDATE_OPENAPI=1 (see the comment at the top of this test).");
        string committed = (await File.ReadAllTextAsync(SpecPath)).ReplaceLineEndings("\n");
        Assert.True(committed == generated,
            "docs/api/openapi.json is out of date with the API. Regenerate it with UPDATE_OPENAPI=1 (see the comment at the top of this test) and commit the result.");
    }

    // Drops the test server's URL and fixes formatting, so the file only changes when the API does
    private static string Normalize(string json)
    {
        var doc = JsonNode.Parse(json)!.AsObject();
        doc.Remove("servers");
        return doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n";
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FromTheKitchen_API.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository root (FromTheKitchen_API.slnx).");
    }
}
