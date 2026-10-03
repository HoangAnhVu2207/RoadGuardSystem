using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Reports;

namespace RoadGuardSystem.Repositories.Extensions;

public static class Huy01ReporterPersistenceExtensions
{
    public static IServiceCollection AddHuy01ReporterIntakePersistence(this IServiceCollection services)
    {
        services.TryAddScoped<IReporterReportRepository, ReporterReportRepository>();
        return services;
    }

    public static IServiceCollection AddHuy01ReporterCasePersistence(this IServiceCollection services)
    {
        services.AddHuy01ReporterIntakePersistence();
        services.TryAddScoped<IReporterLifecycleRepository, ReporterLifecycleRepository>();
        services.TryAddScoped<RoadGuardSystem.Repositories.Cases.ICaseWorkflowRepository, RoadGuardSystem.Repositories.Implementations.Cases.CaseWorkflowRepository>();
        return services;
    }

    public static IServiceCollection AddHuy01ReporterPersistence(this IServiceCollection services)
    {
        services.AddHuy01ReporterCasePersistence();
        services.TryAddScoped<RoadGuardSystem.Repositories.Defects.ICandidateDecisionRepository, RoadGuardSystem.Repositories.Implementations.Defects.CandidateDecisionRepository>();
        return services;
    }
}
