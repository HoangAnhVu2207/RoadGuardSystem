using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class PasswordRecoveryRequestConfiguration : IEntityTypeConfiguration<PasswordRecoveryRequest>
{
    public void Configure(EntityTypeBuilder<PasswordRecoveryRequest> builder)
    {
        builder.ToTable("PasswordRecoveryRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();
        builder.Property(request => request.RequestedAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(request => request.CorrelationId).HasColumnType("uniqueidentifier");
        builder.HasIndex(request => new { request.TargetUserId, request.RequestedAtUtc });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(request => request.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
