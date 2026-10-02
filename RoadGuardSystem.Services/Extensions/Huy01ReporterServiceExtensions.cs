using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Extensions;

public static class Huy01ReporterServiceExtensions
{
    public static IServiceCollection AddHuy01ReporterServices(this IServiceCollection services)
    {
        services.AddScoped<IReporterReportService, ReporterReportService>();
        services.AddScoped<IReporterLifecycleService, ReporterLifecycleService>();
        services.AddScoped<RoadGuardSystem.Services.Cases.ICaseWorkflowService, RoadGuardSystem.Services.Implementations.Cases.CaseWorkflowService>();
        services.AddScoped<RoadGuardSystem.Services.Defects.ICandidateDecisionService, RoadGuardSystem.Services.Implementations.Defects.CandidateDecisionService>();
        return services;
    }
}
