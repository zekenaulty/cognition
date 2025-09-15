using Cognition.Jobs;
using Cognition.Data.Relational;
using Cognition.Clients;
using Hangfire;
using Hangfire.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
    ?? "Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres";

// Configure Hangfire with PostgreSQL storage
builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
          {
              SchemaName = "hangfire",
              InvisibilityTimeout = TimeSpan.FromMinutes(30),
              QueuePollInterval = TimeSpan.FromSeconds(0.25),
              PrepareSchemaIfNecessary = true
          });
});

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "default" };
    options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
    options.HeartbeatInterval        = TimeSpan.FromSeconds(1);
    options.ServerCheckInterval      = TimeSpan.FromSeconds(1);
    options.SchedulePollingInterval  = TimeSpan.FromSeconds(1);
});

// Configure global retry policy
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
{
    Attempts = 3,
    DelaysInSeconds = new[] { 5, 15, 60 }
});

// Bridge config settings into environment variables for clients that read from Environment
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

// Register Db + clients so jobs can use the same services as API
builder.Services.AddCognitionDb(builder.Configuration);
builder.Services.AddCognitionClients();

// Register example + concrete jobs and recurring registration
builder.Services.AddTransient<ExampleJob>();
builder.Services.AddTransient<TextJobs>();
builder.Services.AddTransient<ImageJobs>();
builder.Services.AddHostedService<RecurringJobsRegistrar>();

var host = builder.Build();
host.Run();
