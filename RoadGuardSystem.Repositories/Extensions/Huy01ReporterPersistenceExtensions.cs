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
        services.TryAddScoped<RoadGuardSystem.Repositories.Defects.IDefectWorkflowRepository, RoadGuardSystem.Repositories.Implementations.Defects.DefectWorkflowRepository>();
        services.TryAddScoped<RoadGuardSystem.Repositories.Labels.ITrainingLabelRepository, RoadGuardSystem.Repositories.Implementations.Labels.TrainingLabelRepository>();
        services.TryAddScoped<RoadGuardSystem.Repositories.Reporting.IReportingRepository, RoadGuardSystem.Repositories.Implementations.Reporting.ReportingRepository>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<RoadGuardSystem.Repositories.Retention.IRetentionInventoryContributor,
            RoadGuardSystem.Repositories.Implementations.Retention.Huy01RetentionInventoryContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<RoadGuardSystem.Repositories.Retention.IRetentionInventoryContributor,
            RoadGuardSystem.Repositories.Implementations.Retention.Huy02InspectionRetentionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<RoadGuardSystem.Repositories.Retention.IRetentionInventoryContributor,
            RoadGuardSystem.Repositories.Retention.ExportRetentionInventoryContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<RoadGuardSystem.Repositories.Retention.IRetentionInventoryContributor,
            RoadGuardSystem.Repositories.Retention.AiRetentionInventoryContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<RoadGuardSystem.Repositories.Retention.IRetentionInventoryContributor,
            RoadGuardSystem.Repositories.Retention.ReporterIntakeRetentionInventoryContributor>());
        services.TryAddScoped<RoadGuardSystem.Repositories.Retention.IRetentionRepository,
            RoadGuardSystem.Repositories.Retention.RetentionRepository>();
        services.TryAddScoped<RoadGuardSystem.Repositories.Retention.IRetentionInventoryRepository,
            RoadGuardSystem.Repositories.Retention.RetentionInventoryRepository>();
        services.TryAddScoped<RoadGuardSystem.Repositories.Exports.IExportRepository,
            RoadGuardSystem.Repositories.Exports.ExportRepository>();
        services.TryAddScoped<RoadGuardSystem.Repositories.Processing.IAnh02AiRepository,
            RoadGuardSystem.Repositories.Processing.Anh02AiRepository>();
        services.TryAddSingleton<RoadGuardSystem.Repositories.Storage.IAnh02ArtifactStore,
            RoadGuardSystem.Repositories.Storage.MinioAnh02ArtifactStore>();
        return services;
    }
}
