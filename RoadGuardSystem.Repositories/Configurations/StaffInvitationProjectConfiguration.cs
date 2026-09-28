using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class StaffInvitationProjectConfiguration : IEntityTypeConfiguration<StaffInvitationProject>
{
    public void Configure(EntityTypeBuilder<StaffInvitationProject> builder)
    {
        builder.ToTable("StaffInvitationProjects");
        builder.HasKey(item => new { item.InvitationId, item.ProjectId });
        builder.HasOne<StaffInvitation>()
            .WithMany()
            .HasForeignKey(item => item.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(item => item.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
