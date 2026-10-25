using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services;
using GhostDataAudit.Api.Services.Interfaces;
using GhostDataAudit.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register application services
builder.Services.AddSingleton<IDataLoaderService, DataLoaderService>();
builder.Services.AddSingleton<IIdentityResolverService, IdentityResolverService>();
builder.Services.AddSingleton<IRequestClassifierService, RequestClassifierService>();
builder.Services.AddSingleton<IRuleEngineService, RuleEngineService>();
builder.Services.AddSingleton<IStepBuilderService, StepBuilderService>();
builder.Services.AddSingleton<IStepValidator, StepValidator>();
builder.Services.AddSingleton<IOutputWriterService, OutputWriterService>();
builder.Services.AddSingleton<IAuditOrchestratorService, AuditOrchestratorService>();

// Configure Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Ghost Data Still Exists - Deletion Audit API",
        Version = "v1",
        Description = "ASP.NET Core Web API for the HackerRank Orchestrate Ghost Data Still Exists challenge audit agent."
    });
});

var app = builder.Build();

// Helper to resolve dataset directory safely across different environments
static string ResolveDatasetDirectory(IConfiguration configuration)
{
    var configPath = configuration["Dataset:Path"];
    if (!string.IsNullOrEmpty(configPath) && Directory.Exists(configPath))
        return Path.GetFullPath(configPath);

    var current = Directory.GetCurrentDirectory();
    string[] candidates = [
        Path.Combine(current, "dataset"),
        Path.Combine(current, "../dataset"),
        Path.Combine(current, "../../dataset"),
        Path.Combine(AppContext.BaseDirectory, "../../../../../dataset"),
        Path.Combine(AppContext.BaseDirectory, "../../../../dataset")
    ];

    foreach (var candidate in candidates)
    {
        if (Directory.Exists(candidate))
            return Path.GetFullPath(candidate);
    }

    return Path.GetFullPath(Path.Combine(current, "dataset"));
}

// Check for CLI execution mode
if (args.Contains("--generate-output") || args.Contains("--run") || args.Contains("--audit-all"))
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var orchestrator = app.Services.GetRequiredService<IAuditOrchestratorService>();
    var writer = app.Services.GetRequiredService<IOutputWriterService>();
    var config = app.Services.GetRequiredService<IConfiguration>();

    var datasetDir = ResolveDatasetDirectory(config);
    logger.LogInformation("Starting Ghost Data Deletion Audit. Dataset Directory: {Dir}", datasetDir);

    await orchestrator.InitializeDatasetAsync(datasetDir);
    var results = await orchestrator.AuditAllAsync();

    // Determine output path (repo root output.csv)
    var repoRoot = Path.GetFullPath(Path.Combine(datasetDir, ".."));
    var outputPath = Path.Combine(repoRoot, "output.csv");

    await writer.WriteOutputCsvAsync(outputPath, results);
    logger.LogInformation("Successfully generated output.csv with {Count} cases at {Path}", results.Count, outputPath);

    var completeCount = results.Count(r => r.DeletionStatus == "COMPLETE");
    var incompleteCount = results.Count(r => r.DeletionStatus == "INCOMPLETE");
    var notAppCount = results.Count(r => r.DeletionStatus == "NOT_APPLICABLE");

    Console.WriteLine($"[AUDIT COMPLETE] Total: {results.Count} | COMPLETE: {completeCount} | INCOMPLETE: {incompleteCount} | NOT_APPLICABLE: {notAppCount}");
    return;
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ghost Data Audit API v1");
    c.RoutePrefix = "swagger";
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"An unexpected error occurred.\"}");
    });
});

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Challenge = "HackerRank Orchestrate - Ghost Data Still Exists",
    Status = "Healthy",
    ApiBase = "/api/audit",
    SwaggerUI = "/swagger"
}));

app.Run();
