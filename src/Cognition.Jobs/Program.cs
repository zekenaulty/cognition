using Cognition.Jobs;
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
              InvisibilityTimeout = TimeSpan.FromMinutes(5),
              QueuePollInterval = TimeSpan.FromSeconds(5),
              PrepareSchemaIfNecessary = true
          });
});

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "default" };
    options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
});

// Configure global retry policy
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
{
    Attempts = 3,
    DelaysInSeconds = new[] { 5, 15, 60 }
});

// Register example job and recurring registration
builder.Services.AddTransient<ExampleJob>();
builder.Services.AddHostedService<RecurringJobsRegistrar>();

var host = builder.Build();
host.Run();
