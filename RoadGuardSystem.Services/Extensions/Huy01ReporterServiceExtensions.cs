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
        services.TryAddScoped<RoadGuardSystem.Services.Candidates.ICandidateMatchingService, RoadGuardSystem.Services.Implementations.Defects.CandidateMatchingService>();
        services.TryAddScoped<RoadGuardSystem.Services.Defects.IDefectWorkflowService, RoadGuardSystem.Services.Implementations.Defects.DefectWorkflowService>();
        services.TryAddScoped<RoadGuardSystem.Services.Labels.ITrainingLabelService, RoadGuardSystem.Services.Implementations.Labels.TrainingLabelService>();
        services.TryAddScoped<RoadGuardSystem.Services.Implementations.Labels.TrainingLabelExportReader>();
        services.TryAddScoped<RoadGuardSystem.Services.Integration.IApprovedTrainingLabelReader>(provider => provider.GetRequiredService<RoadGuardSystem.Services.Implementations.Labels.TrainingLabelExportReader>());
        services.TryAddScoped<RoadGuardSystem.Services.Integration.ITrainingSourceAccessReader>(provider => provider.GetRequiredService<RoadGuardSystem.Services.Implementations.Labels.TrainingLabelExportReader>());
        services.TryAddScoped<RoadGuardSystem.Services.Integration.ICaseDefectReadReader, RoadGuardSystem.Services.Implementations.Integration.CaseDefectReadReader>();
        services.TryAddScoped<RoadGuardSystem.Services.Reporting.IReportingService, RoadGuardSystem.Services.Reporting.ReportingService>();
        services.TryAddScoped<RoadGuardSystem.Services.Exports.IExportRenderer, RoadGuardSystem.Services.Exports.ExportRenderer>();
        services.TryAddScoped<RoadGuardSystem.Services.Exports.IExportService, RoadGuardSystem.Services.Exports.ExportService>();
        return services;
    }
}
