using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("Sessions", table =>
        {
            table.HasCheckConstraint(
                "CK_Sessions_DeviceMetadataJson_Json",
                "[DeviceMetadataJson] IS NULL OR ISJSON([DeviceMetadataJson]) = 1");
            table.HasCheckConstraint(
                "CK_Sessions_ExpiresAt",
                "[ExpiresAt] > [IssuedAt]");
            table.HasCheckConstraint(
                "CK_Sessions_RevokedAt",
                "[RevokedAt] IS NULL OR [RevokedAt] >= [IssuedAt]");
        });

        builder.HasKey(session => session.Id);

        builder.Property(session => session.UserId)
            .IsRequired();

        builder.HasOne(session => session.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(session => session.IssuedAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();

        builder.Property(session => session.DeviceMetadataJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(session => session.ExpiresAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();

        builder.Property(session => session.RevokedAt)
            .HasColumnType("datetimeoffset(7)");

        builder.HasIndex(session => session.UserId)
            .HasDatabaseName("IX_Sessions_UserId");
    }
}
