using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Devices;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DroneDeviceConfiguration : IEntityTypeConfiguration<DroneDevice>
{
    public void Configure(EntityTypeBuilder<DroneDevice> builder)
    {
        builder.ToTable("DroneDevices", table =>
        {
            table.HasCheckConstraint("CK_DroneDevices_Status", "[Status] IN (1, 2, 3)");
        });

        builder.HasKey(device => device.Id);
        builder.Property(device => device.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(device => device.SerialNo)
            .HasMaxLength(120)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(device => device.SerialNo)
            .IsUnique()
            .HasDatabaseName("UX_DroneDevices_SerialNo");
        builder.Property(device => device.Model)
            .HasMaxLength(120);
        builder.Property(device => device.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(device => device.ChecklistVersion)
            .HasMaxLength(50)
            .IsUnicode(false);
    }
}
