using Cognition.Jobs;
using Cognition.Data.Relational;
using Cognition.Domains.Documents;
using Cognition.Domains.Relational;
using Cognition.Workflows.Relational;
using Cognition.Clients;
using Cognition.Data.Vectors.OpenSearch.OpenSearch;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Clients.Tools.Planning;
using Hangfire;
using Hangfire.PostgreSql;
using Rebus.ServiceProvider;
using Rebus.Config;

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

// Rebus configuration
var rebusConn = connectionString;
builder.Services.AddRebus(config =>
    config
        .Transport(t => t.UsePostgreSql(rebusConn, "rebus_messages", "cognition-jobs"))
        .Subscriptions(s => s.StoreInPostgres(rebusConn, "rebus_subscriptions"))
);

// Register Db + clients so jobs can use the same services as API
builder.Services.AddCognitionDb(builder.Configuration);
builder.Services.AddCognitionDomainsDb(builder.Configuration);
builder.Services.AddCognitionWorkflowsDb(builder.Configuration);
builder.Services.AddCognitionDomainsDocuments(builder.Configuration);
builder.Services.AddCognitionClients();
builder.Services.AddCognitionOpenSearchVectors(builder.Configuration);
builder.Services.AddCognitionTools();
builder.Services.AddScoped<Cognition.Clients.Retrieval.IRetrievalService, Cognition.Clients.Retrieval.RetrievalService>();
builder.Services.AddScoped<IFictionWeaverJobClient, FictionWeaverJobClient>();
builder.Services.AddTransient<IFictionBacklogScheduler, FictionBacklogScheduler>();
builder.Services.AddOptions<PlannerQuotaOptions>()
    .Bind(builder.Configuration.GetSection(PlannerQuotaOptions.SectionName));
builder.Services.AddOptions<FictionAutomationOptions>()
    .Bind(builder.Configuration.GetSection(FictionAutomationOptions.SectionName));

var workflowEventsEnabled = builder.Configuration.GetValue("WorkflowEvents:Enabled", true);
builder.Services.AddScoped(sp => new WorkflowEventLogger(sp.GetRequiredService<CognitionDbContext>(), workflowEventsEnabled));
builder.Services.AddScoped<IFictionLifecycleTelemetry, FictionLifecycleWorkflowTelemetry>();


// Register example + concrete jobs and recurring registration
builder.Services.AddTransient<TextJobs>();
builder.Services.AddTransient<FictionWeaverJobs>();
builder.Services.AddTransient<ImageJobs>();
builder.Services.AddHostedService<RecurringJobsRegistrar>();

// Register event handlers for Rebus
builder.Services.AddTransient<UserMessageHandler>();
builder.Services.AddTransient<PlanHandler>();
builder.Services.AddTransient<PlanReadyHandler>();
builder.Services.AddTransient<ToolExecutionHandler>();

// Register SignalRNotifier and inject into ResponseHandler
builder.Services.AddSingleton(sp => new SignalRNotifier("http://localhost:5000/hub/chat"));
builder.Services.AddSingleton<IPlanProgressNotifier>(sp => sp.GetRequiredService<SignalRNotifier>());
builder.Services.AddTransient<ResponseHandler>();
builder.Services.AddTransient<HubForwarderHandler>();

// Register SignalRNotifier as a hosted service
builder.Services.AddHostedService<SignalRNotifierHostedService>();

var host = builder.Build();
host.Run();

// Log Rebus input queue name at startup
{
    var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("RebusStartup");
    logger.LogInformation("Rebus input queue: cognition-jobs");
}
