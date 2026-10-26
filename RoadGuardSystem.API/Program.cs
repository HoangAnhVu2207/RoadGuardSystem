using Asp.Versioning.ApiExplorer;
using RoadGuardSystem.API.Extensions;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.API.Workers;

var builder = WebApplication.CreateBuilder(SandboxSeedCommand.NormalizeArguments(args));
if (Environment.GetEnvironmentVariable("ROADGUARD_HANDOFF") == "1")
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Handoff is Development only.");
    builder.Configuration.AddJsonFile("/handoff/config.json", optional: false, reloadOnChange: false);
    var handoffOrigins = HandoffBootstrap.ResolveOrigins(Environment.GetEnvironmentVariable("RG_FE_ORIGINS"), builder.Configuration.GetSection("Handoff:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173", "http://localhost:8081"]);
    builder.Services.AddCors(options => options.AddPolicy("HandoffLocal", policy => policy
        .WithOrigins(handoffOrigins)
        .AllowAnyMethod().AllowAnyHeader().AllowCredentials()));
}

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Development.local.json",
        optional: true,
        reloadOnChange: true);
}

// Add services to the container.
builder.Services.AddControllers(options => options.Filters.AddService<WebCookieActivityFilter>());
builder.Services.AddApiPlatformServices(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddRoadGuardSandbox(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddRoadGuardSeeding(
    includeDevelopmentUsers: builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue("RoadGuardDatabase:SeedDevelopmentUsers", false));

if (builder.Environment.IsDevelopment() &&
    !string.IsNullOrWhiteSpace(builder.Configuration[$"{MinioStorageOptions.SectionName}:Endpoint"]))
{
    builder.Services.AddHostedService<UploadVerificationWorker>();
}

if (builder.Configuration.GetValue($"{UploadSessionOptions.SectionName}:RecoveryEnabled", false))
{
    builder.Services.AddHostedService<MultipartRecoveryWorker>();
}

if (builder.Configuration.GetValue("Anh02:WorkersEnabled", false))
{
    builder.Services.AddHostedService<ExportWorker>();
    builder.Services.AddHostedService<RetentionEvaluationWorker>();
    if ((builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") || builder.Environment.IsEnvironment("Test"))
        && builder.Configuration.GetValue("Anh02:MockEnabled", false))
        builder.Services.AddHostedService<AiMockRunWorker>();
}

var app = builder.Build();
await HandoffBootstrap.InitializeAsync(app);

if (await SandboxSeedCommand.TryExecuteAsync(app, args) is int seedExitCode)
{
    Environment.ExitCode = seedExitCode;
    await app.DisposeAsync();
    return;
}

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
        options.ConfigObject.AdditionalItems["withCredentials"] = true;
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

// The isolated handoff intentionally exposes HTTP for internal health and bearer clients.
if (Environment.GetEnvironmentVariable("ROADGUARD_HANDOFF") != "1") app.UseHttpsRedirection();
if (Environment.GetEnvironmentVariable("ROADGUARD_HANDOFF") == "1") app.UseCors("HandoffLocal");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<WebCookieRequestMiddleware>();

// 4. Process liveness health check (unversioned)
app.MapHealthChecks("/health");
HandoffBootstrap.MapReadiness(app);

// 5. Versioned API controllers
app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in API tests.
// Must remain at the end of the file and must not change runtime behavior.
public partial class Program { }
