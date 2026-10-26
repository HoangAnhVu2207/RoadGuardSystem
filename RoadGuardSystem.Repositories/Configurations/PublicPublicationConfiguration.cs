using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Cases;
namespace RoadGuardSystem.Repositories.Configurations;

public sealed class PublicPublicationConfiguration : IEntityTypeConfiguration<PublicPublication>
{
    public void Configure(EntityTypeBuilder<PublicPublication> builder)
    {
        builder.ToTable("PublicPublications"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DamageCategory).HasMaxLength(40).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.ProjectId, x.DefectId });
        builder.HasOne<RoadGuardSystem.BusinessObjects.Projects.Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadGuardSystem.BusinessObjects.Defects.Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IncidentCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CaseConclusion>().WithMany().HasForeignKey(x => x.ConclusionId).OnDelete(DeleteBehavior.Restrict);
    }
}
