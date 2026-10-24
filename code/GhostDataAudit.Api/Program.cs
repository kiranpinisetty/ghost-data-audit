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
builder.Services.AddSingleton<IAuditOrchestratorService, AuditOrchestratorService>();
builder.Services.AddSingleton<IOutputWriterService, OutputWriterService>();
builder.Services.AddSingleton<IStepValidator, StepValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    ApiBase = "/api/audit"
}));

app.Run();
