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
using Cognition.Api.Infrastructure.Swagger;
using Cognition.Api.Infrastructure.Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add EF Core Postgres DbContext
builder.Services.AddCognitionDb(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCognitionClients();
builder.Services.AddCognitionTools();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Avoid schema ID collisions for nested/duplicate DTO names
    c.CustomSchemaIds(type =>
    {
        var fullName = type.FullName ?? type.Name;
        // Replace '+' from nested types with '.' to keep it readable
        return fullName.Replace("+", ".");
    });
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cognition API",
        Version = "v1"
    });

    // JWT Bearer authentication so the UI shows the Authorize button
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT access token only (without 'Bearer')"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Remove security from endpoints marked [AllowAnonymous]
    c.OperationFilter<AllowAnonymousOperationFilter>();
});

// Bridge configuration secrets into environment variables for libraries that read from Environment
void SetEnvFromConfig(string envName, params string[] configKeys)
{
    foreach (var key in configKeys)
    {
        var val = builder.Configuration[key];
        if (!string.IsNullOrWhiteSpace(val))
        {
            Environment.SetEnvironmentVariable(envName, val);
            break;
        }
    }
}
SetEnvFromConfig("OPENAI_API_KEY", "OPENAI_API_KEY", "OPENAI_KEY");
SetEnvFromConfig("OPENAI_BASE_URL", "OPENAI_BASE_URL");
SetEnvFromConfig("GOOGLE_API_KEY", "GOOGLE_API_KEY");
SetEnvFromConfig("GEMINI_API_KEY", "GEMINI_API_KEY", "GOOGLE_API_KEY");
SetEnvFromConfig("OLLAMA_BASE_URL", "OLLAMA_BASE_URL");
SetEnvFromConfig("GITHUB_TOKEN", "GITHUB_TOKEN");

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
builder.Services.AddSingleton<JobStorage>(sp => Hangfire.JobStorage.Current);
builder.Services.AddSingleton<IHangfireRunner, HangfireRunner>();

// JWT Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]
    ?? Environment.GetEnvironmentVariable("JWT__Secret")
    ?? JwtOptions.DevFallbackSecret; // ensure >= 32 bytes

// Guard: never allow dev fallback secret in production
if (builder.Environment.IsProduction() && string.Equals(jwtSecret, JwtOptions.DevFallbackSecret, StringComparison.Ordinal))
{
    throw new InvalidOperationException("JWT secret is using the development fallback in Production. Configure Jwt:Secret or JWT__Secret.");
}

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
        // Enable lifetime validation so expired tokens are rejected
        ValidateLifetime = true,
        // Optionally wire issuer/audience when configured
        ValidateIssuer = !string.IsNullOrWhiteSpace(jwtSection["Issuer"]),
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = !string.IsNullOrWhiteSpace(jwtSection["Audience"]),
        ValidAudience = jwtSection["Audience"],
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

// Log env status (masked) for quick verification in logs
{
    string Mask(string? s) => string.IsNullOrEmpty(s) ? "" : new string('*', Math.Max(0, s.Length - Math.Min(4, s.Length))) + (s.Length >= 4 ? s[^4..] : s);
    var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("EnvStatus");
    var openaiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? Environment.GetEnvironmentVariable("OPENAI_KEY");
    var openaiBase = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
    var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    var geminiBase = Environment.GetEnvironmentVariable("GEMINI_BASE_URL");
    var ollamaBase = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL");
    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    log.LogInformation("Env: OpenAI hasKey={Has} base={Base}", !string.IsNullOrWhiteSpace(openaiKey), openaiBase ?? "(default)");
    log.LogInformation("Env: Gemini hasKey={Has} base={Base}", !string.IsNullOrWhiteSpace(geminiKey), geminiBase ?? "(default)");
    log.LogInformation("Env: Ollama base={Base}", ollamaBase ?? "(unset)");
    log.LogInformation("Env: GitHub hasToken={Has}", !string.IsNullOrWhiteSpace(githubToken));
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
