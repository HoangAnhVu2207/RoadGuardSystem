using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyRequestConfiguration : IEntityTypeConfiguration<SurveyRequest>
{
    public void Configure(EntityTypeBuilder<SurveyRequest> builder)
    {
        builder.ToTable("SurveyRequests", table =>
        {
            table.HasTrigger("TR_SurveyRequests_ScopeIntegrity");
            table.HasCheckConstraint("CK_SurveyRequests_SurveyType", "[SurveyType] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_SurveyRequests_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)");
            table.HasCheckConstraint(
                "CK_SurveyRequests_Cancellation",
                "([Status] = 9 AND [CancelledAt] IS NOT NULL AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0) " +
                "OR ([Status] <> 9 AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL)");
        });

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(request => request.ProjectId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(request => request.RoadSectionId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(request => request.SurveyPlanId)
            .HasColumnType("uniqueidentifier");
        builder.Property(request => request.RequestedByUserId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(request => request.SurveyType)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(request => request.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(request => request.RequestedAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();
        builder.Property(request => request.CancelledAt)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(request => request.CancellationReason)
            .HasColumnType("nvarchar(max)");
        builder.HasIndex(request => new { request.ProjectId, request.RoadSectionId, request.Status })
            .HasDatabaseName("IX_SurveyRequests_ProjectRoadStatus");
        builder.HasIndex(request => request.SurveyPlanId)
            .IsUnique()
            .HasFilter("[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10)")
            .HasDatabaseName("UX_SurveyRequests_ActivePlan");
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(request => request.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSection>()
            .WithMany()
            .HasForeignKey(request => request.RoadSectionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SurveyPlan>()
            .WithMany()
            .HasForeignKey(request => request.SurveyPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(request => request.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
