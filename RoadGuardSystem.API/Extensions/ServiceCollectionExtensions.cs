using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Extensions;
using RoadGuardSystem.Services.Options;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Projects;
using RoadGuardSystem.Services.Warranties;
using RoadGuardSystem.Services.Surveys;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

/// <summary>
/// Service collection extension methods for registering API platform foundation services.
/// Keeps Program.cs clean and modular.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiPlatformServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        services.AddRoadGuardAuthenticationApplication(configuration, isProduction);

        var jwtOptions = new JwtOptions();
        configuration.GetRequiredSection(JwtOptions.SectionName).Bind(jwtOptions);
        var jwtValidation = new JwtOptionsValidator().Validate(JwtOptions.SectionName, jwtOptions);
        if (jwtValidation.Failed)
        {
            throw new OptionsValidationException(
                JwtOptions.SectionName,
                typeof(JwtOptions),
                jwtValidation.Failures!);
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => JwtBearerConfiguration.Configure(options, jwtOptions));
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ProjectAuthorizationPolicies.WorkPackageRead, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ProjectAccessRequirement());
            });
        });
        services.AddScoped<IProjectScopeGuard, ProjectScopeGuard>();
        services.AddScoped<IProjectWorkPackageService, ProjectWorkPackageService>();
        services.AddScoped<IProjectCreationService, ProjectCreationService>();
        services.AddScoped<IProjectUpdateService, ProjectUpdateService>();
        services.AddScoped<IPrimaryProjectManagerService, PrimaryProjectManagerService>();
        services.AddScoped<IWarrantyCreationService, WarrantyCreationService>();
        services.AddScoped<IRoadSectionVersionService, RoadSectionVersionService>();
        services.AddScoped<ISurveyAssignmentService, SurveyAssignmentService>();
        services.AddScoped<ISurveyPlanningService, SurveyPlanningService>();
        services.AddScoped<IAuthorizationHandler, ProjectAccessAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProjectAuthorizationMiddlewareResultHandler>();

        // 1. Health Checks (unversioned process liveness check, no DB or Docker required)
        services.AddHealthChecks();

        // 2. ProblemDetails RFC 7807/9110 configuration
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var httpContext = context.HttpContext;
                var correlationId = httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string
                    ?? httpContext.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].ToString();

                if (string.IsNullOrWhiteSpace(correlationId) || !Guid.TryParse(correlationId, out _))
                {
                    correlationId = Guid.NewGuid().ToString();
                    httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
                    httpContext.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = correlationId;
                }

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Instance ??= httpContext.Request.Path;

                // Enforce generic message and code on internal server errors (500)
                if (context.ProblemDetails.Status is null or >= StatusCodes.Status500InternalServerError)
                {
                    context.ProblemDetails.Status = StatusCodes.Status500InternalServerError;
                    context.ProblemDetails.Title = "Internal Server Error";
                    context.ProblemDetails.Detail = "An unexpected internal server error occurred.";
                    context.ProblemDetails.Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
                    context.ProblemDetails.Extensions["code"] = ApiErrorCodes.InternalError;
                }
                else if (context.ProblemDetails.Status == StatusCodes.Status404NotFound)
                {
                    context.ProblemDetails.Title ??= "Not Found";
                    context.ProblemDetails.Detail ??= "The requested resource was not found.";
                    context.ProblemDetails.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
                    context.ProblemDetails.Extensions["code"] = ApiErrorCodes.NotFound;
                }
                else if (context.ProblemDetails.Status == StatusCodes.Status400BadRequest)
                {
                    context.ProblemDetails.Title ??= "Bad Request";
                    context.ProblemDetails.Type ??= "https://tools.ietf.org/html/rfc9110#section-15.5.1";
                    if (!context.ProblemDetails.Extensions.ContainsKey("code"))
                    {
                        context.ProblemDetails.Extensions["code"] = ApiErrorCodes.ValidationError;
                    }
                }
                else if (context.ProblemDetails.Status == StatusCodes.Status405MethodNotAllowed)
                {
                    context.ProblemDetails.Title ??= "Method Not Allowed";
                    context.ProblemDetails.Type ??= "https://tools.ietf.org/html/rfc9110#section-15.5.6";
                    context.ProblemDetails.Extensions["code"] = ApiErrorCodes.MethodNotAllowed;
                }
                else if (context.ProblemDetails.Status == StatusCodes.Status415UnsupportedMediaType)
                {
                    context.ProblemDetails.Title ??= "Unsupported Media Type";
                    context.ProblemDetails.Type ??= "https://tools.ietf.org/html/rfc9110#section-15.5.16";
                    context.ProblemDetails.Extensions["code"] = ApiErrorCodes.UnsupportedMediaType;
                }

                // Security invariant: Purge any stack trace, internal exception type, or machine path
                context.ProblemDetails.Extensions.Remove("exception");
                context.ProblemDetails.Extensions.Remove("stackTrace");
                context.ProblemDetails.Extensions.Remove("trace");

                if (context.ProblemDetails.Detail != null &&
                    (context.ProblemDetails.Detail.Contains(":\\", StringComparison.Ordinal) ||
                     context.ProblemDetails.Detail.Contains("System.Text.Json", StringComparison.OrdinalIgnoreCase) ||
                     context.ProblemDetails.Detail.Contains("Exception", StringComparison.OrdinalIgnoreCase)))
                {
                    context.ProblemDetails.Detail = "An error occurred while processing the request.";
                }
            };
        });

        // 3. API Model Validation configuration
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var httpContext = context.HttpContext;
                var correlationId = httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string
                    ?? httpContext.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].ToString();

                if (string.IsNullOrWhiteSpace(correlationId) || !Guid.TryParse(correlationId, out _))
                {
                    correlationId = Guid.NewGuid().ToString();
                    httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
                    httpContext.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = correlationId;
                }

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Detail = "The request payload failed validation.",
                    Instance = httpContext.Request.Path,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };

                problemDetails.Extensions["code"] = ApiErrorCodes.ValidationError;
                problemDetails.Extensions["correlationId"] = correlationId;

                var errors = new Dictionary<string, string[]>();
                foreach (var (key, value) in context.ModelState)
                {
                    if (value.Errors.Count > 0)
                    {
                        var sanitized = value.Errors
                            .Select(e => SanitizeValidationErrorMessage(e.ErrorMessage, e.Exception))
                            .ToArray();
                        errors[key] = sanitized;
                    }
                }
                problemDetails.Extensions["errors"] = errors;

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        // 4. API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // 5. OpenAPI / Swagger documentation
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    private static string SanitizeValidationErrorMessage(string errorMessage, Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "Invalid field value or format.";
        }

        if (errorMessage.Contains("System.Text.Json", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains(":\\", StringComparison.Ordinal))
        {
            return "Malformed JSON syntax or invalid value format.";
        }

        return errorMessage;
    }
}
