using Asp.Versioning.ApiExplorer;
using RoadGuardSystem.API.Extensions;
using RoadGuardSystem.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddApiPlatformServices(builder.Configuration, builder.Environment.IsProduction());

var app = builder.Build();

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
