using System;
using Cognition.Data.Relational;
using Cognition.Clients;
using Cognition.Api.Infrastructure;
using Cognition.Api.Infrastructure.Diagnostics;
using Cognition.Api.Infrastructure.Alerts;
using Cognition.Api.Infrastructure.OpenSearch;
using Cognition.Api.Infrastructure.Planning;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
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
using Cognition.Jobs;
using Microsoft.EntityFrameworkCore;
using Rebus.ServiceProvider;
using Rebus.Config;
using Cognition.Data.Vectors.OpenSearch.OpenSearch;
using Cognition.Clients.Configuration;
using Cognition.Clients.Tools.Planning;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddControllersAsServices();
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Add EF Core Postgres DbContext
builder.Services.AddCognitionDb(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCognitionClients();
builder.Services.AddCognitionOpenSearchVectors(builder.Configuration);
builder.Services.AddHttpClient("opensearch-bootstrap", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OpenSearchVectorsOptions>>().Value;
    client.BaseAddress = new Uri(options.Url);
});
builder.Services.AddHttpClient("ops-alerts");
builder.Services.AddCognitionTools();
builder.Services.AddScoped<IPlannerHealthService, PlannerHealthService>();
builder.Services.AddScoped<IOpenSearchBootstrapper, OpenSearchBootstrapper>();
builder.Services.AddScoped<IOpenSearchDiagnosticsService, OpenSearchDiagnosticsService>();
builder.Services.AddOptions<ScopePathOptions>()
    .Bind(builder.Configuration.GetSection(ScopePathOptions.SectionName));
builder.Services.AddOptions<PlannerCritiqueOptions>()
    .Bind(builder.Configuration.GetSection(PlannerCritiqueOptions.SectionName));
builder.Services.AddOptions<OpsAlertingOptions>()
    .Bind(builder.Configuration.GetSection(OpsAlertingOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<PlannerQuotaOptions>()
    .Bind(builder.Configuration.GetSection(PlannerQuotaOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<OpsAlertingOptions>, OpsAlertingOptionsValidator>();
builder.Services.AddScoped<Cognition.Api.Infrastructure.ScopePath.ScopePathBackfillService>();

var rateLimitSection = builder.Configuration.GetSection(ApiRateLimitingOptions.SectionName);
builder.Services.Configure<ApiRateLimitingOptions>(rateLimitSection);
var apiRateLimitingOptions = rateLimitSection.Get<ApiRateLimitingOptions>() ?? new ApiRateLimitingOptions();

if (apiRateLimitingOptions.MaxRequestBodyBytes is > 0)
{
    builder.Services.Configure<KestrelServerOptions>(opts =>
    {
        opts.Limits.MaxRequestBodySize = apiRateLimitingOptions.MaxRequestBodyBytes;
    });
    builder.Services.Configure<IISServerOptions>(opts =>
    {
        opts.MaxRequestBodySize = apiRateLimitingOptions.MaxRequestBodyBytes;
    });
}

builder.Services.AddRateLimiter(options =>
{
    if (apiRateLimitingOptions.Global?.IsEnabled == true)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetFixedWindowLimiter("global", _ => apiRateLimitingOptions.Global!.ToLimiterOptions()));
    }

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("RateLimiting");
        var correlationId = httpContext.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId) && !httpContext.Response.Headers.ContainsKey(CorrelationConstants.HeaderName))
        {
            httpContext.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
        }

        logger?.LogWarning("Rate limit exceeded for {Path} (correlationId={CorrelationId})", httpContext.Request.Path, correlationId ?? string.Empty);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var retrySeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
            ? (int?)Math.Ceiling(retry.TotalSeconds)
            : null;

        await httpContext.Response.WriteAsJsonAsync(new
        {
            code = "rate_limited",
            message = "Request quota exceeded. Please retry later.",
            retryAfterSeconds = retrySeconds,
            correlationId
        }, cancellationToken);
    };

    if (apiRateLimitingOptions.PerUser?.IsEnabled == true)
    {
        options.AddPolicy(ApiRateLimitingOptions.UserPolicyName, httpContext =>
        {
            var userId = ApiRateLimiterPartitionKeys.ResolveUserId(httpContext);
            if (string.IsNullOrEmpty(userId))
            {
                return RateLimitPartition.GetNoLimiter("user:none");
            }

            return RateLimitPartition.GetFixedWindowLimiter($"user:{userId}", _ => apiRateLimitingOptions.PerUser!.ToLimiterOptions());
        });
    }

    if (apiRateLimitingOptions.PerPersona?.IsEnabled == true)
    {
        options.AddPolicy(ApiRateLimitingOptions.PersonaPolicyName, httpContext =>
        {
            var personaId = ApiRateLimiterPartitionKeys.ResolvePersonaId(httpContext);
            if (string.IsNullOrEmpty(personaId))
            {
                return RateLimitPartition.GetNoLimiter("persona:none");
            }

            return RateLimitPartition.GetFixedWindowLimiter($"persona:{personaId}", _ => apiRateLimitingOptions.PerPersona!.ToLimiterOptions());
        });
    }

    if (apiRateLimitingOptions.PerAgent?.IsEnabled == true)
    {
        options.AddPolicy(ApiRateLimitingOptions.AgentPolicyName, httpContext =>
        {
            var agentId = ApiRateLimiterPartitionKeys.ResolveAgentId(httpContext);
            if (string.IsNullOrEmpty(agentId))
            {
                return RateLimitPartition.GetNoLimiter("agent:none");
            }

            return RateLimitPartition.GetFixedWindowLimiter($"agent:{agentId}", _ => apiRateLimitingOptions.PerAgent!.ToLimiterOptions());
        });
    }
});

// Background knowledge indexer is disabled for now; use the API endpoint to trigger indexing on-demand.
// Wire AgentService DI for API controllers
builder.Services.AddScoped<Cognition.Clients.Agents.IAgentService, Cognition.Clients.Agents.AgentService>();
// Retrieval service (scope-enforcing RAG entrypoint)
builder.Services.AddScoped<Cognition.Clients.Retrieval.IRetrievalService, Cognition.Clients.Retrieval.RetrievalService>();
builder.Services.AddScoped<IFictionWeaverJobClient, FictionWeaverJobClient>();
builder.Services.AddSingleton<IPlannerAlertPublisher, OpsWebhookAlertPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
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

// Rebus configuration
var rebusConn = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres";
builder.Services.AddRebus(config =>
    config
        .Transport(t => t.UsePostgreSql(rebusConn, "rebus_messages", "cognition-api"))
        .Subscriptions(s => s.StoreInPostgres(rebusConn, "rebus_subscriptions"))
        //.Options(o => o.SimpleRetryStrategy("rebus-errors"))
);


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
    //string Mask(string? s) => string.IsNullOrEmpty(s) ? "" : new string('*', Math.Max(0, s.Length - Math.Min(4, s.Length))) + (s.Length >= 4 ? s[^4..] : s);
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

app.UseRequestCorrelation();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}

// Require auth for all API controllers, but allow SPA/static/Swagger/Hangfire/SignalR anonymously
var controllerEndpoints = app.MapControllers().RequireAuthorization();
if (apiRateLimitingOptions.PerUser?.IsEnabled == true)
{
    controllerEndpoints = controllerEndpoints.RequireRateLimiting(ApiRateLimitingOptions.UserPolicyName);
}
if (apiRateLimitingOptions.PerPersona?.IsEnabled == true)
{
    controllerEndpoints = controllerEndpoints.RequireRateLimiting(ApiRateLimitingOptions.PersonaPolicyName);
}
if (apiRateLimitingOptions.PerAgent?.IsEnabled == true)
{
    controllerEndpoints = controllerEndpoints.RequireRateLimiting(ApiRateLimitingOptions.AgentPolicyName);
}

var chatHub = app.MapHub<Cognition.Api.Controllers.ChatHub>("/hub/chat");
if (apiRateLimitingOptions.PerUser?.IsEnabled == true)
{
    chatHub = chatHub.RequireRateLimiting(ApiRateLimitingOptions.UserPolicyName);
}
if (apiRateLimitingOptions.PerPersona?.IsEnabled == true)
{
    chatHub = chatHub.RequireRateLimiting(ApiRateLimitingOptions.PersonaPolicyName);
}
if (apiRateLimitingOptions.PerAgent?.IsEnabled == true)
{
    chatHub = chatHub.RequireRateLimiting(ApiRateLimitingOptions.AgentPolicyName);
}
_ = chatHub;

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

// Apply pending migrations then seed default data (user + personas)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CognitionDbContext>();
    try { db.Database.Migrate(); } catch { }
}
// Seed default data (user + personas)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
    await StartupDataSeeder.SeedAsync(app.Services, logger);
}

// Log Rebus input queue name at startup
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("RebusStartup");
    var rebusBus = app.Services.GetService<Rebus.Bus.IBus>();
    logger.LogInformation("Rebus input queue: cognition-api");
}

app.Run();
