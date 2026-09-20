using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", table =>
        {
            table.HasCheckConstraint("CK_Projects_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_Projects_DateRange",
                "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
            table.HasCheckConstraint(
                "CK_Projects_EngineeringUtmSrid",
                "[EngineeringUtmSrid] IS NULL OR [EngineeringUtmSrid] IN (32648, 32649)");
        });

        builder.HasKey(project => project.Id);
        builder.Property(project => project.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(project => project.ProjectCode).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.HasIndex(project => project.ProjectCode).IsUnique().HasDatabaseName("UX_Projects_ProjectCode");
        builder.Property(project => project.Name).HasMaxLength(255).IsRequired();
        builder.Property(project => project.Description).HasColumnType("nvarchar(max)");
        builder.Property(project => project.EngineeringUtmSrid).HasColumnType("int");
        builder.Property(project => project.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(project => project.StartDate).HasColumnType("date");
        builder.Property(project => project.EndDate).HasColumnType("date");
        builder.Property(project => project.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
    }
}
