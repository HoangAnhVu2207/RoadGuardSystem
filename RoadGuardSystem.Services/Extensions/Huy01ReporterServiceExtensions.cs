using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Extensions;

public static class Huy01ReporterServiceExtensions
{
    public static IServiceCollection AddHuy01ReporterServices(this IServiceCollection services)
    {
        services.AddScoped<IReporterReportService, ReporterReportService>();
        return services;
    }
}
