using FromTheKitchen_API.Data;
using FromTheKitchen_API.Utility;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FromTheKitchen_API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => {
    // Retries transient errors, e.g. Azure SQL serverless waking up from auto-pause
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.EnableRetryOnFailure());
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// The frontend runs on another origin. It sends a Bearer header, not cookies, so no AllowCredentials.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// Kept out of source control: user secrets locally, an environment variable (ApiSettings__Secret) elsewhere
var key = builder.Configuration.GetValue<string>("ApiSettings:Secret")
    ?? throw new InvalidOperationException(
        "ApiSettings:Secret is not set. Run: dotnet user-secrets set \"ApiSettings:Secret\" \"<a random key of at least 32 bytes>\"");
builder.Services.AddAuthentication(u =>
{
    u.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    u.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(u =>
{
    u.RequireHttpsMetadata = false;
    u.SaveToken = true;
    u.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.Zero
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityScheme>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // ponytail: tests run on SQLite, which can't apply SQL Server migrations. Migrating at startup
    // is fine for one instance; move it to a migration bundle in the pipeline before scaling out.
    if (db.Database.IsSqlServer()) db.Database.Migrate(); else db.Database.EnsureCreated();
    await AdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseDefaultFiles();
app.UseStaticFiles();

// Uploads live outside wwwroot when ImageStorage:Path is set; serve them under /images next to the seeded ones
string imagesFolder = SD.ImagesFolder(app.Configuration, app.Environment);
if (Path.GetFullPath(imagesFolder) != Path.GetFullPath(Path.Combine(app.Environment.WebRootPath, "images")))
{
    Directory.CreateDirectory(imagesFolder);
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(imagesFolder), RequestPath = "/images" });
}
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// The React app (copied into wwwroot by the deploy) owns routes like /cart and /my-orders,
// so a refresh or a shared link on those gets index.html. Unknown /api paths and missing files stay 404.
app.MapFallback(async context =>
{
    var index = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
    if (!index.Exists || context.Request.Path.StartsWithSegments("/api") || Path.HasExtension(context.Request.Path.Value))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(index);
});

app.Run();


internal sealed class BearerSecurityScheme(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            var requirement = new Dictionary<string, IOpenApiSecurityScheme>
            {
                [JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                }
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirement;
        }

        document.Info = new()
        {
            Title = "From The Kitchen API",
            Version = "v1",
            Description = "API for From The Kitchen application",
        };
    }
}