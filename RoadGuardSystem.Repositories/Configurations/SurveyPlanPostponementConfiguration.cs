using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyPlanPostponementConfiguration : IEntityTypeConfiguration<SurveyPlanPostponement>
{
    public void Configure(EntityTypeBuilder<SurveyPlanPostponement> builder)
    {
        builder.ToTable("SurveyPlanPostponements", table =>
        {
            table.HasTrigger("TR_SurveyPlanPostponements_AppendOnly");
        });

        builder.HasKey(postponement => postponement.Id);
        builder.Property(postponement => postponement.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(postponement => postponement.SurveyPlanId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(postponement => postponement.PostponedAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();
        builder.Property(postponement => postponement.Reason)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.Property(postponement => postponement.NewPlannedStartAt)
            .HasColumnType("datetimeoffset(7)");
        builder.HasIndex(postponement => new { postponement.SurveyPlanId, postponement.PostponedAt })
            .HasDatabaseName("IX_SurveyPlanPostponements_PlanId_PostponedAt");
        builder.HasOne<SurveyPlan>()
            .WithMany()
            .HasForeignKey(postponement => postponement.SurveyPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
