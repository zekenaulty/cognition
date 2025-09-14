using Cognition.Data.Relational;
using Cognition.Clients;
using Cognition.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add EF Core Postgres DbContext
builder.Services.AddCognitionDb(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCognitionClients();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for Vite dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", p =>
        p.WithOrigins(
             "http://localhost:5173",
             "https://localhost:5173"
         )
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// Hangfire Dashboard (no server here). Uses same Postgres storage.
var hfConn = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(hfConn);
});

// JWT Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]
    ?? Environment.GetEnvironmentVariable("JWT__Secret")
    ?? "dev-secret-change-me-please-change-32bytes!dev"; // ensure >= 32 bytes
JwtOptions.Secret = jwtSecret; // expose for token issuance
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cognition API v1");
        c.RoutePrefix = "swagger";
    });
}

// Only redirect to HTTPS if an HTTPS endpoint is configured
var urlsSetting = builder.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var httpsPort = builder.Configuration["ASPNETCORE_HTTPS_PORT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT");
var hasHttps = (!string.IsNullOrEmpty(urlsSetting) && urlsSetting.Contains("https://", StringComparison.OrdinalIgnoreCase))
               || !string.IsNullOrEmpty(httpsPort);
if (hasHttps)
{
    app.UseHttpsRedirection();
}

// Serve SPA static files built by Vite (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}

// Require auth for all API controllers, but allow SPA/static/Swagger/Hangfire anonymously
app.MapControllers().RequireAuthorization();

// Expose Hangfire Dashboard
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
else
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new LocalOnlyDashboardAuthorizationFilter() }
    });
}

// Fallback to index.html for client-side routing if SPA assets exist
var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (File.Exists(Path.Combine(wwwroot, "index.html")))
{
    app.MapFallbackToFile("/index.html");
}

// Seed default data (user + personas)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
    await StartupDataSeeder.SeedAsync(app.Services, logger);
}

app.Run();
