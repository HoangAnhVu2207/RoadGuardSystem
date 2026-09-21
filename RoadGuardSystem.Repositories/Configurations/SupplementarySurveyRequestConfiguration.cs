using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SupplementarySurveyRequestConfiguration : IEntityTypeConfiguration<SupplementarySurveyRequest>
{
    public void Configure(EntityTypeBuilder<SupplementarySurveyRequest> builder)
    {
        builder.ToTable("SupplementarySurveyRequests", table =>
        {
            table.HasCheckConstraint("CK_SupplementarySurveyRequests_RoundNo", "[RoundNo] > 0");
            table.HasCheckConstraint("CK_SupplementarySurveyRequests_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7)");
            table.HasCheckConstraint("CK_SupplementarySurveyRequests_RequestedScope_JsonObject", "ISJSON([RequestedScope]) = 1 AND LEFT(LTRIM([RequestedScope]), 1) = '{'");
            table.HasCheckConstraint(
                "CK_SupplementarySurveyRequests_Approval",
                "([ApprovedByUserId] IS NULL AND [ApprovedAt] IS NULL) OR ([ApprovedByUserId] IS NOT NULL AND [ApprovedAt] IS NOT NULL)");
        });

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(request => request.SurveyId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(request => request.SurveyRequestId).HasColumnType("uniqueidentifier");
        builder.Property(request => request.RequestedByUserId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(request => request.Reason).HasColumnType("nvarchar(1000)").IsRequired();
        builder.Property(request => request.RequestedScope).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(request => request.RoundNo).HasColumnType("int").IsRequired();
        builder.Property(request => request.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(request => request.ApprovedByUserId).HasColumnType("uniqueidentifier");
        builder.Property(request => request.ApprovedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(request => request.SourcePreservationNote).HasColumnType("nvarchar(max)");
        builder.HasIndex(request => new { request.SurveyId, request.RoundNo }).IsUnique()
            .HasDatabaseName("UX_SupplementarySurveyRequests_SurveyRound");
        builder.HasIndex(request => request.SurveyRequestId).HasDatabaseName("IX_SupplementarySurveyRequests_SurveyRequestId");
        builder.HasOne<Survey>().WithMany().HasForeignKey(request => request.SurveyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SurveyRequest>().WithMany().HasForeignKey(request => request.SurveyRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(request => request.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(request => request.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
