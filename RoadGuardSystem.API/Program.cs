using Asp.Versioning.ApiExplorer;
using RoadGuardSystem.API.Extensions;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.API.Workers;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Development.local.json",
        optional: true,
        reloadOnChange: true);
    // Deployment/demo overrides must retain their normal precedence over personal local settings.
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddApiPlatformServices(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddRoadGuardSeeding(
    includeDevelopmentUsers: builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue("RoadGuardDatabase:SeedDevelopmentUsers", false));

if (builder.Environment.IsDevelopment() &&
    !string.IsNullOrWhiteSpace(builder.Configuration[$"{MinioStorageOptions.SectionName}:Endpoint"]))
{
    builder.Services.AddHostedService<UploadVerificationWorker>();
}

// Explicit opt-in avoids polling un-migrated deployments; no migration is applied here.
if (builder.Configuration.GetValue("Anh02:WorkersEnabled", false))
{
    builder.Services.AddHostedService<ExportWorker>();
    builder.Services.AddHostedService<RetentionEvaluationWorker>();
    if ((builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") || builder.Environment.IsEnvironment("Test"))
        && builder.Configuration.GetValue("Anh02:MockEnabled", false))
        builder.Services.AddHostedService<AiMockRunWorker>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue("RoadGuardDatabase:InitializeOnStartup", false))
{
    await DbInitializer.InitializeAsync(app.Services);
}

// 1. Correlation ID middleware runs earliest so all downstream errors/responses have correlation ID
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. Exception handling and status code pages for uniform ProblemDetails
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<ApiVersioningValidationMiddleware>();

// 3. Configure OpenAPI in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        var descriptions = provider.ApiVersionDescriptions;
        if (descriptions.Count == 0)
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
        }
        else
        {
            foreach (var description in descriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
            }
        }
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// 4. Process liveness health check (unversioned)
app.MapHealthChecks("/health");

// 5. Versioned API controllers
app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in API tests.
// Must remain at the end of the file and must not change runtime behavior.
public partial class Program { }
