using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Reports;

namespace RoadGuardSystem.Repositories.Extensions;

public static class Huy01ReporterPersistenceExtensions
{
    public static IServiceCollection AddHuy01ReporterPersistence(this IServiceCollection services)
    {
        services.AddScoped<IReporterReportRepository, ReporterReportRepository>();
        services.AddScoped<IReporterLifecycleRepository, ReporterLifecycleRepository>();
        services.AddScoped<RoadGuardSystem.Repositories.Cases.ICaseWorkflowRepository, RoadGuardSystem.Repositories.Implementations.Cases.CaseWorkflowRepository>();
        services.AddScoped<RoadGuardSystem.Repositories.Defects.ICandidateDecisionRepository, RoadGuardSystem.Repositories.Implementations.Defects.CandidateDecisionRepository>();
        return services;
    }
}
