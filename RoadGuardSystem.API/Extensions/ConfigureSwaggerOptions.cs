using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

/// <summary>
/// Configures SwaggerGenOptions dynamically using IApiVersionDescriptionProvider.
/// Ensures v1 OpenAPI document is always configured and accurately describes versioned routes.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "RoadGuard JWT access token."
        });
        options.OperationFilter<AuthorizeOperationFilter>();

        var descriptions = _provider.ApiVersionDescriptions;
        if (descriptions.Count == 0)
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RoadGuard API v1",
                Version = "v1",
                Description = "RoadGuard System API platform."
            });
        }
        else
        {
            var hasV1 = false;
            foreach (var description in descriptions)
            {
                if (description.GroupName.Equals("v1", StringComparison.OrdinalIgnoreCase))
                {
                    hasV1 = true;
                }
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }

            if (!hasV1)
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "RoadGuard API v1",
                    Version = "v1",
                    Description = "RoadGuard System API platform."
                });
            }
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = $"RoadGuard API {description.GroupName}",
            Version = description.ApiVersion.ToString(),
            Description = "RoadGuard System API platform."
        };

        if (description.IsDeprecated)
        {
            info.Description += " (This API version has been deprecated).";
        }

        return info;
    }
}
