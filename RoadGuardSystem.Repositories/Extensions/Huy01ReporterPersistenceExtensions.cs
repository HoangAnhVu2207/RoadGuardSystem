using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Reports;

namespace RoadGuardSystem.Repositories.Extensions;

public static class Huy01ReporterPersistenceExtensions
{
    public static IServiceCollection AddHuy01ReporterPersistence(this IServiceCollection services)
    {
        services.AddScoped<IReporterReportRepository, ReporterReportRepository>();
        return services;
    }
}
