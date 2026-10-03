using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Extensions;

public static class Huy01ReporterServiceExtensions
{
    public static IServiceCollection AddHuy01ReporterIntakeServices(this IServiceCollection services)
    {
        services.TryAddScoped<IReporterReportService, ReporterReportService>();
        return services;
    }

    public static IServiceCollection AddHuy01ReporterCaseServices(this IServiceCollection services)
    {
        services.AddHuy01ReporterIntakeServices();
        services.TryAddScoped<IReporterLifecycleService, ReporterLifecycleService>();
        services.TryAddScoped<RoadGuardSystem.Services.Cases.ICaseWorkflowService, RoadGuardSystem.Services.Implementations.Cases.CaseWorkflowService>();
        return services;
    }

    public static IServiceCollection AddHuy01ReporterServices(this IServiceCollection services)
    {
        services.AddHuy01ReporterCaseServices();
        services.TryAddScoped<RoadGuardSystem.Services.Defects.ICandidateDecisionService, RoadGuardSystem.Services.Implementations.Defects.CandidateDecisionService>();
        return services;
    }
}
