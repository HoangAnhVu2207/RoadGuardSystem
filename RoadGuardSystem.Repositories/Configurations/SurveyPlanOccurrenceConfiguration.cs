using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyPlanOccurrenceConfiguration : IEntityTypeConfiguration<SurveyPlanOccurrence>
{
    public void Configure(EntityTypeBuilder<SurveyPlanOccurrence> builder)
    {
        builder.ToTable("SurveyPlanOccurrences", table =>
            table.HasCheckConstraint("CK_SurveyPlanOccurrences_DateRange", "[PlannedEndAt] > [PlannedStartAt]"));
        builder.HasKey(occurrence => occurrence.Id);
        builder.Property(occurrence => occurrence.Id).ValueGeneratedNever();
        builder.Property(occurrence => occurrence.RowVersion).IsRowVersion();
        builder.HasIndex(occurrence => new { occurrence.SeriesPlanId, occurrence.PlannedStartAt }).IsUnique();
        builder.HasIndex(occurrence => occurrence.SurveyPlanId).IsUnique();
        builder.HasOne<SurveyPlan>().WithMany().HasForeignKey(occurrence => occurrence.SeriesPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SurveyPlan>().WithMany().HasForeignKey(occurrence => occurrence.SurveyPlanId).OnDelete(DeleteBehavior.Restrict);
    }
}
